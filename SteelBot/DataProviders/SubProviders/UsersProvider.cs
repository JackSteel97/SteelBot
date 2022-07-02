using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Sentry;
using SteelBot.Database;
using SteelBot.Database.Models;
using SteelBot.Database.Models.Users;
using SteelBot.Helpers.Sentry;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using User = SteelBot.Database.Models.Users.User;

namespace SteelBot.DataProviders.SubProviders;

public class UsersProvider
{
    private readonly ILogger<UsersProvider> _logger;
    private readonly IDbContextFactory<SteelBotContext> _dbContextFactory;
    private readonly IHub _sentry;
    private readonly AsyncReaderWriterLock _lock = new AsyncReaderWriterLock();

    /// <summary>
    /// Indexed on the user's discord id & guild id
    /// The same user has one entry per server they are in.
    /// </summary>
    private readonly Dictionary<(ulong guildId, ulong userId), User> _usersByDiscordIdAndServer;

    public UsersProvider(ILogger<UsersProvider> logger, IDbContextFactory<SteelBotContext> contextFactory, IHub sentry)
    {
        _logger = logger;
        _dbContextFactory = contextFactory;
        _sentry = sentry;

        _usersByDiscordIdAndServer = LoadUserData();
    }

    private Dictionary<(ulong, ulong), User> LoadUserData()
    {
        var transaction = _sentry.StartNewConfiguredTransaction("StartUp", nameof(LoadUserData));
        var result = new Dictionary<(ulong, ulong), User>();
        using (_lock.WriterLock())
        {
            _logger.LogInformation("Loading data from database: Users");
            using (var db = _dbContextFactory.CreateDbContext())
            {
                result = db.Users
                    .Include(u => u.Guild)
                    .Include(u => u.CurrentRankRole)
                    .AsNoTracking()
                    .ToDictionary(u => (u.Guild.DiscordId, u.DiscordId));
            }
        }

        transaction.Finish();
        return result;

    }

    public bool BotKnowsUser(ulong guildId, ulong userId)
    {
        using (_lock.ReaderLock())
        {
            return BotKnowsUserCore(guildId, userId);
        }
    }

    public bool TryGetUser(ulong guildId, ulong userId, out User user)
    {
        using (_lock.ReaderLock())
        {
            return TryGetUserCore(guildId, userId, out user);
        }
    }

    public List<User> GetAllUsers()
    {
        using (_lock.ReaderLock())
        {
            return _usersByDiscordIdAndServer.Values.ToList();
        }
    }

    public List<User> GetUsersInGuild(ulong guildId)
    {
        using (_lock.ReaderLock())
        {
            var lookup = _usersByDiscordIdAndServer.ToLookup(u => u.Key.guildId, u => u.Value);
            // Returns empty collection if guild id not found.
            return lookup[guildId].ToList();
        }
    }

    /// <summary>
    /// Inserts a new user for a given guild.
    /// If the user already exists no insert is performed.
    /// </summary>
    /// <param name="guildId">The Discord id of the guild.</param>
    /// <param name="user">The internal model for the </param>
    public async Task InsertUser(ulong guildId, User user)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(InsertUser));

        using (await _lock.WriterLockAsync())
        {
            if (!BotKnowsUserCore(guildId, user.DiscordId))
            {
                _logger.LogInformation("Writing a new User {UserId} in Guild {GuildId}", user.DiscordId, guildId);

                int writtenCount;
                using (var db = _dbContextFactory.CreateDbContext())
                {
                    db.Users.Add(user);
                    writtenCount = await db.SaveChangesAsync();
                }

                if (writtenCount > 0)
                {
                    _usersByDiscordIdAndServer.Add((guildId, user.DiscordId), user);
                }
                else
                {
                    _logger.LogError("Writing User {UserId} in Guild {GuildId} to the database inserted no entities. The internal cache was not changed.", user.DiscordId, guildId);
                }
            }
        }

        transaction.Finish();
    }

    public async Task RemoveUser(ulong guildId, ulong userId)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(RemoveUser));

        using (await _lock.WriterLockAsync())
        {
            if (TryGetUserCore(guildId, userId, out var user))
            {
                _logger.LogInformation("Deleting a User [{UserId}] in Guild [{GuildId}]", userId, guildId);

                int writtenCount;
                using (var db = _dbContextFactory.CreateDbContext())
                {
                    db.Users.Remove(user);
                    writtenCount = await db.SaveChangesAsync();
                }

                if (writtenCount > 0)
                {
                    _usersByDiscordIdAndServer.Remove((guildId, userId));
                }
                else
                {
                    _logger.LogError("Deleting User [{UserId}] in Guild [{GuildId}] from the database altered no entities. The internal cache was not changed.", userId, guildId);
                }
            }
        }

        transaction.Finish();
    }

    public async Task UpdateRankRole(ulong guildId, ulong userId, RankRole newRole)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(UpdateRankRole));

        if (TryGetUser(guildId, userId, out var user))
        {
            _logger.LogInformation("Updating RankRole for User {UserId} in Guild {GuildId} to {NewRole}", userId, guildId, newRole?.RoleName);

            // Clone user to avoid making change to cache till db change confirmed.
            var copyOfUser = user.Clone();

            copyOfUser.CurrentRankRole = newRole;
            copyOfUser.CurrentRankRoleRowId = newRole != default ? newRole.RowId : null;

            await UpdateUser(guildId, copyOfUser);
        }

        transaction.Finish();
    }

    public async Task UpdateUser(ulong guildId, User newUser)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(UpdateUser));

        using (await _lock.WriterLockAsync())
        {
            int writtenCount;
            using (var db = _dbContextFactory.CreateDbContext())
            {
                // To prevent EF tracking issue, grab and alter existing value.
                var original = db.Users.First(u => u.RowId == newUser.RowId);

                var audit = new UserAudit(original, guildId, newUser.CurrentRankRole?.RoleName);
                db.UserAudits.Add(audit);

                db.Entry(original).CurrentValues.SetValues(newUser);
                original.LastUpdated = DateTime.UtcNow;
                db.Users.Update(original);
                writtenCount = await db.SaveChangesAsync();
            }

            // Both audit and actual written?
            if (writtenCount > 1)
            {
                _usersByDiscordIdAndServer[(guildId, newUser.DiscordId)] = newUser;
            }
            else
            {
                _logger.LogError("Updating User {UserId} in Guild {GuildId} did not alter any entities. The internal cache was not changed.", newUser.DiscordId, guildId);
            }
        }

        transaction.Finish();
    }

    private bool BotKnowsUserCore(ulong guildId, ulong userId) => _usersByDiscordIdAndServer.ContainsKey((guildId, userId));

    private bool TryGetUserCore(ulong guildId, ulong userId, out User user) => _usersByDiscordIdAndServer.TryGetValue((guildId, userId), out user);
}