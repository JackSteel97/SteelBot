using DSharpPlus;
using DSharpPlus.Entities;
using SteelBot.Database.Models;
using SteelBot.DataProviders;
using SteelBot.DiscordModules;
using System.Threading.Tasks;

namespace SteelBot.Services
{
    public class UserTrackingService
    {
        private readonly DataCache Cache;
        private readonly DataHelpers DataHelpers;

        public UserTrackingService(DataCache cache, DataHelpers dataHelpers)
        {
            Cache = cache;
            DataHelpers = dataHelpers;
        }

        /// <summary>
        /// Call this for every entry point receiver.
        /// Make sure the bot know about the user so we can update user state when needed.
        /// </summary>
        /// <param name="guildId">Guild the user is in.</param>
        /// <param name="userId">User's discord id.</param>
        public async Task<bool> TrackUser(ulong guildId, DiscordUser user, DiscordGuild discordGuild, DiscordClient client)
        {
            bool continuationAllowed = !user.IsBot && user.Id != client.CurrentUser.Id;

            if (continuationAllowed)
            {
                // Add the guild if it somehow doesn't exist.
                bool guildExists = Cache.Guilds.BotKnowsGuild(guildId);
                if (!guildExists)
                {
                    await Cache.Guilds.UpsertGuild(new Guild(guildId));
                }

                Cache.Guilds.TryGetGuild(guildId, out Guild guild);

                bool userExists = Cache.Users.BotKnowsUser(guildId, user.Id);
                if (!userExists)
                {
                    // Only inserted if the user does not already exist.
                    await Cache.Users.InsertUser(guildId, new User(user.Id, guild.RowId));

                    await DataHelpers.RankRoles.UserLevelledUp(guildId, user.Id, discordGuild);
                }
            }
            else if (user.IsBot && user.Id != client.CurrentUser.Id)
            {
                // Is a bot, but not this bot.
                bool userExists = Cache.Users.BotKnowsUser(guildId, user.Id);
                if (userExists)
                {
                    // This is a bot and we've tracked it, known in Discord API can cause bots to not have the flag set correctly the first time we are notified of their connection.
                    // If we've accidentally tracked this user thinking it was not a bot but it now is, remove it.
                    await Cache.Users.RemoveUser(guildId, user.Id);
                }
            }

            return continuationAllowed;
        }
    }
}