using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.Logging;
using SteelBot.Database;
using SteelBot.Database.Models;
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
                UsersByDiscordIdAndServer = db.Users.AsNoTracking().Include(u => u.Guild).ToDictionary(u => (u.Guild.DiscordId, u.DiscordId));
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

        /// <summary>
        /// Update the last command received date time to the current UTC time.
        /// </summary>
        /// <param name="guildId">Guild discord id.</param>
        /// <param name="userId">User discord id.</param>
        public async Task UpdateLastCommandTime(ulong guildId, ulong userId)
        {
            if (TryGetUser(guildId, userId, out User user))
            {
                Logger.LogInformation($"Updating last command time for User [{userId}] in Guild [{guildId}]");
                // Clone user to avoid making change to cache till db change confirmed.
                User copyOfUser = user.Clone();
                copyOfUser.LastCommandReceived = DateTime.UtcNow;

                await UpdateUser(guildId, copyOfUser);
            }
        }

        /// <summary>
        /// Set the muted state of a user to total muted time.
        /// </summary>
        /// <param name="guildId">User's Guild</param>
        /// <param name="userId">User's discord Id</param>
        /// <param name="newStateMuted">Current muted state</param>
        public async Task<bool> UpdateMutedState(ulong guildId, ulong userId, bool newStateMuted)
        {
            bool levelIncreased = false;
            if (TryGetUser(guildId, userId, out User user))
            {
                Logger.LogInformation($"Updating muted state for User [{userId}] in Guild [{guildId}]");
                // Clone user to avoid making change to cache till db change confirmed.
                User copyOfUser = user.Clone();
                copyOfUser.LastActivity = DateTime.UtcNow;
                if (newStateMuted)
                {
                    // User is now muted.
                    copyOfUser.MutedStartTime = DateTime.UtcNow;
                }
                else
                {
                    // User is now unmuted.
                    if (copyOfUser.MutedStartTime.HasValue)
                    {
                        copyOfUser.TimeSpentMuted += DateTime.UtcNow - copyOfUser.MutedStartTime.Value;
                        // Reset start time.
                        copyOfUser.MutedStartTime = null;
                        levelIncreased = copyOfUser.UpdateLevel();
                    }
                }

                await UpdateUser(guildId, copyOfUser);
            }
            return levelIncreased;
        }

        /// <summary>
        /// Set the deafened state of a user to total deafened time.
        /// </summary>
        /// <param name="guildId">User's guild.</param>
        /// <param name="userId">User's discord id.</param>
        /// <param name="newStateDeafened">Current deafened state.</param>
        public async Task<bool> UpdateDeafendedState(ulong guildId, ulong userId, bool newStateDeafened)
        {
            bool levelIncreased = false;
            if (TryGetUser(guildId, userId, out User user))
            {
                Logger.LogInformation($"Updating deafened state for User [{userId}] in Guild [{guildId}]");
                // Clone user to avoid making change to cache till db change confirmed.
                User copyOfUser = user.Clone();
                copyOfUser.LastActivity = DateTime.UtcNow;
                if (newStateDeafened)
                {
                    // User is now deafened.
                    copyOfUser.DeafenedStartTime = DateTime.UtcNow;
                }
                else
                {
                    // User is now un-deafened.
                    if (copyOfUser.DeafenedStartTime.HasValue)
                    {
                        copyOfUser.TimeSpentDeafened += DateTime.UtcNow - copyOfUser.DeafenedStartTime.Value;
                        // Reset start time.
                        copyOfUser.DeafenedStartTime = null;
                        levelIncreased = copyOfUser.UpdateLevel();
                    }
                }

                await UpdateUser(guildId, copyOfUser);
            }
            return levelIncreased;
        }

        /// <summary>
        /// Set the streaming state of a user to total streaming time.
        /// </summary>
        /// <param name="guildId">User's guild.</param>
        /// <param name="userId">User's discord id.</param>
        /// <param name="newStateStreaming">Current streaming state.</param>
        public async Task<bool> UpdateStreamingState(ulong guildId, ulong userId, bool newStateStreaming)
        {
            bool levelIncreased = false;
            if (TryGetUser(guildId, userId, out User user))
            {
                // Only update if it's different from the cached state.
                if ((user.StreamingStartTime.HasValue && !newStateStreaming) || (!user.StreamingStartTime.HasValue && newStateStreaming))
                {
                    Logger.LogInformation($"Updating streaming state for User [{userId}] in Guild [{guildId}]");
                    // Clone user to avoid making change to cache till db change confirmed.
                    User copyOfUser = user.Clone();
                    copyOfUser.LastActivity = DateTime.UtcNow;
                    if (newStateStreaming)
                    {
                        // User is now streaming.
                        copyOfUser.StreamingStartTime = DateTime.UtcNow;
                    }
                    else
                    {
                        // User is now not streaming.
                        if (copyOfUser.StreamingStartTime.HasValue)
                        {
                            copyOfUser.TimeSpentStreaming += DateTime.UtcNow - copyOfUser.StreamingStartTime.Value;
                            // Reset start time.
                            copyOfUser.StreamingStartTime = null;
                            levelIncreased = copyOfUser.UpdateLevel();
                        }
                    }

                    await UpdateUser(guildId, copyOfUser);
                }
            }
            return levelIncreased;
        }

        /// <summary>
        /// Set the voice state of a user to total voice time.
        /// </summary>
        /// <param name="guildId">User's guild.</param>
        /// <param name="userId">User's discord id.</param>
        /// <param name="inVoiceChannel">If the user is in a voice channel.</param>
        public async Task<bool> UpdateVoiceState(ulong guildId, ulong userId, bool inVoiceChannel)
        {
            bool levelIncreased = false;
            if (TryGetUser(guildId, userId, out User user))
            {
                Logger.LogInformation($"Updating voice state for User [{userId}] in Guild [{guildId}]");
                // Clone user to avoid making change to cache till db change confirmed.
                User copyOfUser = user.Clone();
                copyOfUser.LastActivity = DateTime.UtcNow;
                if (inVoiceChannel)
                {
                    // User is now in a voice channel.
                    copyOfUser.VoiceStartTime = DateTime.UtcNow;
                }
                else
                {
                    // User is now not in a voice channel.
                    if (copyOfUser.VoiceStartTime.HasValue)
                    {
                        copyOfUser.TimeSpentInVoice += DateTime.UtcNow - copyOfUser.VoiceStartTime.Value;
                        // Reset start time.
                        copyOfUser.VoiceStartTime = null;
                        levelIncreased = copyOfUser.UpdateLevel();
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