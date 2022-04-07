using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using SteelBot.Database;
using SteelBot.Database.Models;
using SteelBot.Database.Models.Users;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders
{
    public class UsersProvider
    {
        private readonly ILogger<UsersProvider> Logger;
        private readonly IDbContextFactory<SteelBotContext> DbContextFactory;
        private readonly AppConfigurationService AppConfigurationService;
        private readonly AsyncReaderWriterLock Lock = new AsyncReaderWriterLock();

        /// <summary>
        /// Indexed on the user's discord id & guild id
        /// The same user has one entry per server they are in.
        /// </summary>
        private readonly Dictionary<(ulong guildId, ulong userId), User> UsersByDiscordIdAndServer;

        public UsersProvider(ILogger<UsersProvider> logger, IDbContextFactory<SteelBotContext> contextFactory, AppConfigurationService appConfigurationService)
        {
            Logger = logger;
            DbContextFactory = contextFactory;
            AppConfigurationService = appConfigurationService;

            UsersByDiscordIdAndServer = LoadUserData();
        }

        private Dictionary<(ulong, ulong), User> LoadUserData()
        {
            using (Lock.WriterLock())
            {
                var result = new Dictionary<(ulong, ulong), User>();
                Logger.LogInformation("Loading data from database: Users");
                using (SteelBotContext db = DbContextFactory.CreateDbContext())
                {
                    result = db.Users
                        .Include(u => u.Guild)
                        .Include(u => u.CurrentRankRole)
                        .AsNoTracking()
                        .ToDictionary(u => (u.Guild.DiscordId, u.DiscordId));
                }
                return result;
            }
        }

        public bool BotKnowsUser(ulong guildId, ulong userId)
        {
            using (Lock.ReaderLock())
            {
                return BotKnowsUserCore(guildId, userId);
            }
        }

        public bool TryGetUser(ulong guildId, ulong userId, out User user)
        {
            using (Lock.ReaderLock())
            {
                return TryGetUserCore(guildId, userId, out user);
            }
        }

        public List<User> GetAllUsers()
        {
            using (Lock.ReaderLock())
            {
                return UsersByDiscordIdAndServer.Values.ToList();
            }
        }

        public List<User> GetUsersInGuild(ulong guildId)
        {
            using (Lock.ReaderLock())
            {
                ILookup<ulong, User> lookup = UsersByDiscordIdAndServer.ToLookup(u => u.Key.guildId, u => u.Value);
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
            using (await Lock.WriterLockAsync())
            {
                if (!BotKnowsUserCore(guildId, user.DiscordId))
                {
                    Logger.LogInformation("Writing a new User {UserId} in Guild {GuildId}", user.DiscordId, guildId);

                    int writtenCount;
                    using (SteelBotContext db = DbContextFactory.CreateDbContext())
                    {
                        db.Users.Add(user);
                        writtenCount = await db.SaveChangesAsync();
                    }

                    if (writtenCount > 0)
                    {
                        UsersByDiscordIdAndServer.Add((guildId, user.DiscordId), user);
                    }
                    else
                    {
                        Logger.LogError("Writing User {UserId} in Guild {GuildId} to the database inserted no entities. The internal cache was not changed.", user.DiscordId, guildId);
                    }
                }
            }
        }

        public async Task RemoveUser(ulong guildId, ulong userId)
        {
            using (await Lock.WriterLockAsync())
            {
                if (TryGetUserCore(guildId, userId, out User user))
                {
                    Logger.LogInformation("Deleting a User [{UserId}] in Guild [{GuildId}]", userId, guildId);

                    int writtenCount;
                    using (SteelBotContext db = DbContextFactory.CreateDbContext())
                    {
                        db.Users.Remove(user);
                        writtenCount = await db.SaveChangesAsync();
                    }

                    if (writtenCount > 0)
                    {
                        UsersByDiscordIdAndServer.Remove((guildId, userId));
                    }
                    else
                    {
                        Logger.LogError("Deleting User [{UserId}] in Guild [{GuildId}] from the database altered no entities. The internal cache was not changed.", userId, guildId);
                    }
                }
            }
        }

        public async Task UpdateRankRole(ulong guildId, ulong userId, RankRole newRole)
        {
            if (TryGetUser(guildId, userId, out User user))
            {
                Logger.LogInformation("Updating RankRole for User {UserId} in Guild {GuildId} to {NewRole}", userId, guildId, newRole?.RoleName);

                // Clone user to avoid making change to cache till db change confirmed.
                User copyOfUser = user.Clone();

                copyOfUser.CurrentRankRole = newRole;
                if (newRole != default)
                {
                    copyOfUser.CurrentRankRoleRowId = newRole.RowId;
                }
                else
                {
                    copyOfUser.CurrentRankRoleRowId = null;
                }

                await UpdateUser(guildId, copyOfUser);
            }
        }

        public async Task UpdateUser(ulong guildId, User newUser)
        {
            using (await Lock.WriterLockAsync())
            {
                int writtenCount;
                using (SteelBotContext db = DbContextFactory.CreateDbContext())
                {
                    // To prevent EF tracking issue, grab and alter existing value.
                    User original = db.Users.First(u => u.RowId == newUser.RowId);

                    var audit = new UserAudit(original, guildId, newUser.CurrentRankRole.RoleName);
                    db.UserAudits.Add(audit);

                    db.Entry(original).CurrentValues.SetValues(newUser);
                    original.LastUpdated = DateTime.UtcNow;
                    db.Users.Update(original);
                    writtenCount = await db.SaveChangesAsync();
                }

                // Both audit and actual written?
                if (writtenCount > 1)
                {
                    UsersByDiscordIdAndServer[(guildId, newUser.DiscordId)] = newUser;
                }
                else
                {
                    Logger.LogError("Updating User {UserId} in Guild {GuildId} did not alter any entities. The internal cache was not changed.", newUser.DiscordId, guildId);
                }
            }
        }

        private bool BotKnowsUserCore(ulong guildId, ulong userId)
        {
            return UsersByDiscordIdAndServer.ContainsKey((guildId, userId));
        }

        private bool TryGetUserCore(ulong guildId, ulong userId, out User user)
        {
            return UsersByDiscordIdAndServer.TryGetValue((guildId, userId), out user);
        }
    }
}