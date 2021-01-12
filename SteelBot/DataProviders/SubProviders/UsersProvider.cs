using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.Logging;
using SteelBot.Database;
using SteelBot.Database.Models;
using SteelBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders
{
    public class UsersProvider
    {
        private readonly ILogger<UsersProvider> Logger;
        private readonly IDbContextFactory<SteelBotContext> DbContextFactory;

        /// <summary>
        /// Indexed on the user's discord id & guild id
        /// The same user has one entry per server they are in.
        /// </summary>
        private Dictionary<(ulong guildId, ulong userId), User> UsersByDiscordIdAndServer;

        public UsersProvider(ILogger<UsersProvider> logger, IDbContextFactory<SteelBotContext> contextFactory)
        {
            Logger = logger;
            DbContextFactory = contextFactory;

            UsersByDiscordIdAndServer = new Dictionary<(ulong, ulong), User>();
            LoadUserData();
        }

        private void LoadUserData()
        {
            Logger.LogInformation("Loading data from database: Users");
            using (var db = DbContextFactory.CreateDbContext())
            {
                UsersByDiscordIdAndServer = db.Users
                    .Include(u => u.Guild)
                    .Include(u => u.CurrentRankRole)
                    .AsNoTracking()
                    .ToDictionary(u => (u.Guild.DiscordId, u.DiscordId));
            }
        }

        public bool BotKnowsUser(ulong guildId, ulong userId)
        {
            return UsersByDiscordIdAndServer.ContainsKey((guildId, userId));
        }

        public bool TryGetUser(ulong guildId, ulong userId, out User user)
        {
            return UsersByDiscordIdAndServer.TryGetValue((guildId, userId), out user);
        }

        public List<User> GetUsersInGuild(ulong guildId)
        {
            ILookup<ulong, User> lookup = UsersByDiscordIdAndServer.ToLookup(u => u.Key.guildId, u => u.Value);
            // Returns empty collection if guild id not found.
            return lookup[guildId].ToList();
        }

        /// <summary>
        /// Inserts a new user for a given guild.
        /// If the user already exists no insert is performed.
        /// </summary>
        /// <param name="guildId">The Discord id of the guild.</param>
        /// <param name="user">The internal model for the </param>
        public async Task InsertUser(ulong guildId, User user)
        {
            if (!BotKnowsUser(guildId, user.DiscordId))
            {
                Logger.LogInformation($"Writing a new User [{user.DiscordId}] in Guild [{guildId}]");

                int writtenCount;
                using (var db = DbContextFactory.CreateDbContext())
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
                    Logger.LogError($"Writing User [{user.DiscordId}] in Guild [{guildId}] to the datbase inserted no entities. The internal cache was not changed.");
                }
            }
        }

        public async Task UpdateRankRole(ulong guildId, ulong userId, RankRole newRole)
        {
            if (TryGetUser(guildId, userId, out User user))
            {
                Logger.LogInformation($"Updating RankRole for User [{userId}] in Guild [{guildId}] to [{newRole?.RoleName}]");

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

        /// <summary>
        /// Update the message stats counters for a given user in a given guild.
        /// </summary>
        /// <param name="guildId">The user's guild discord id.</param>
        /// <param name="userId">The user's discord id.</param>
        /// <param name="messageLength">Length of the sent message.</param>
        public async Task<bool> UpdateMessageCounters(ulong guildId, ulong userId, int messageLength)
        {
            bool levelIncreased = false;
            if (TryGetUser(guildId, userId, out User user))
            {
                Logger.LogInformation($"Updating message counters for User [{userId}] in Guild [{guildId}]");
                DateTime messageReceivedAt = DateTime.UtcNow;

                // Clone user to avoid making change to cache till db change confirmed.
                User copyOfUser = user.Clone();
                copyOfUser.MessageCount++;
                copyOfUser.TotalMessageLength += Convert.ToUInt64(messageLength);

                // Check the last message that earned xp was more than a minute ago.
                bool lastMessageWasMoreThanAMinuteAgo = (messageReceivedAt - copyOfUser.LastXpEarningMessage.GetValueOrDefault()).TotalSeconds >= 60;
                levelIncreased = copyOfUser.UpdateLevel(lastMessageWasMoreThanAMinuteAgo);

                copyOfUser.LastActivity = messageReceivedAt;
                copyOfUser.LastMessageSent = messageReceivedAt;

                await UpdateUser(guildId, copyOfUser);
            }
            return levelIncreased;
        }

        public async Task<bool> UpdateVoiceStateCounters(ulong guildId, ulong userId, DiscordVoiceState newState)
        {
            DateTime updateTimestamp = DateTime.UtcNow;
            bool levelIncreased = false;
            if (TryGetUser(guildId, userId, out User user))
            {
                Logger.LogDebug($"Updating voice state for User [{userId}] in Guild [{guildId}]");
                // Clone user to avoid making changes to cache till db change completed.
                User copyOfUser = user.Clone();
                copyOfUser.LastActivity = updateTimestamp;

                // Can we add to voice time counters?
                if (copyOfUser.VoiceStartTime.HasValue)
                {
                    copyOfUser.TimeSpentInVoice += updateTimestamp - copyOfUser.VoiceStartTime.Value;
                }
                if (copyOfUser.MutedStartTime.HasValue)
                {
                    copyOfUser.TimeSpentMuted += updateTimestamp - copyOfUser.MutedStartTime.Value;
                }
                if (copyOfUser.DeafenedStartTime.HasValue)
                {
                    copyOfUser.TimeSpentDeafened += updateTimestamp - copyOfUser.DeafenedStartTime.Value;
                }
                levelIncreased = copyOfUser.UpdateLevel();

                // Update times.
                if (newState == null || newState.Channel == null)
                {
                    // User has left voice channel - reset all states.
                    copyOfUser.VoiceStartTime = null;
                    copyOfUser.MutedStartTime = null;
                    copyOfUser.DeafenedStartTime = null;
                    // TODO: Add streaming state support.
                }
                else
                {
                    // In voice channel.
                    copyOfUser.VoiceStartTime = updateTimestamp;

                    if (newState.IsSelfMuted)
                    {
                        copyOfUser.MutedStartTime = updateTimestamp;
                    }
                    else
                    {
                        copyOfUser.MutedStartTime = null;
                    }
                    if (newState.IsSelfDeafened)
                    {
                        copyOfUser.DeafenedStartTime = updateTimestamp;
                    }
                    else
                    {
                        copyOfUser.DeafenedStartTime = null;
                    }
                }

                await UpdateUser(guildId, copyOfUser);
            }
            return levelIncreased;
        }

        private async Task UpdateUser(ulong guildId, User newUser)
        {
            int writtenCount;
            using (var db = DbContextFactory.CreateDbContext())
            {
                // To prevent EF tracking issue, grab and alter existing value.
                User original = db.Users.First(u => u.RowId == newUser.RowId);
                db.Entry(original).CurrentValues.SetValues(newUser);
                db.Users.Update(original);
                writtenCount = await db.SaveChangesAsync();
            }

            if (writtenCount > 0)
            {
                UsersByDiscordIdAndServer[(guildId, newUser.DiscordId)] = newUser;
            }
            else
            {
                Logger.LogError($"Updating User [{newUser.DiscordId}] in Guild [{guildId}] did not alter any entities. The internal cache was not changed.");
            }
        }
    }
}