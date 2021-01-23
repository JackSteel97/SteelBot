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
        public async Task TrackUser(ulong guildId, ulong userId, DiscordGuild discordGuild)
        {
            // Add the guild if it somehow doesn't exist.
            bool guildExists = Cache.Guilds.BotKnowsGuild(guildId);
            if (!guildExists)
            {
                await Cache.Guilds.UpsertGuild(new Guild(guildId));
            }

            Cache.Guilds.TryGetGuild(guildId, out Guild guild);

            bool userExists = Cache.Users.BotKnowsUser(guildId, userId);
            if (!userExists)
            {
                // Only inserted if the user does not already exist.
                await Cache.Users.InsertUser(guildId, new User(userId, guild.RowId));

                await DataHelpers.RankRoles.UserLevelledUp(guildId, userId, discordGuild);
            }
        }
    }
}