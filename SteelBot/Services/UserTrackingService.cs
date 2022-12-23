using DSharpPlus;
using DSharpPlus.Entities;
using SteelBot.Database.Models;
using SteelBot.Database.Models.Users;
using SteelBot.DataProviders;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DiscordModules.RankRoles.Helpers;
using System.Threading.Tasks;

namespace SteelBot.Services;

public class UserTrackingService
{
    private readonly DataCache _cache;
    private readonly LevelMessageSender _levelMessageSender;
    private readonly RankRolesProvider _rankRolesProvider;
    private readonly UsersProvider _usersProvider;

    public UserTrackingService(DataCache cache,
        RankRolesProvider rankRolesProvider,
        UsersProvider usersProvider,
        LevelMessageSender levelMessageSender)
    {
        _cache = cache;
        _rankRolesProvider = rankRolesProvider;
        _usersProvider = usersProvider;
        _levelMessageSender = levelMessageSender;
    }

    /// <summary>
    ///     Call this for every entry point receiver.
    ///     Make sure the bot know about the user so we can update user state when needed.
    /// </summary>
    public async Task<bool> TrackUser(ulong guildId, DiscordUser user, DiscordGuild discordGuild, DiscordClient client)
    {
        bool continuationAllowed = !user.IsBot && user.Id != client.CurrentUser.Id;

        if (continuationAllowed)
        {
            // Add the guild if it somehow doesn't exist.
            bool guildExists = _cache.Guilds.BotKnowsGuild(guildId);
            if (!guildExists) await _cache.Guilds.UpsertGuild(new Guild(guildId, discordGuild.Name));

            _cache.Guilds.TryGetGuild(guildId, out var guild);

            bool userExists = _cache.Users.BotKnowsUser(guildId, user.Id);
            if (!userExists)
            {
                // Only inserted if the user does not already exist.
                await _cache.Users.InsertUser(guildId, new User(user.Id, guild.RowId));

                await RankRoleShared.UserLevelledUp(guildId, user.Id, discordGuild, _rankRolesProvider, _usersProvider, _levelMessageSender);
            }
        }
        else if (user.IsBot && user.Id != client.CurrentUser.Id)
        {
            // Is a bot, but not this bot.
            bool userExists = _cache.Users.BotKnowsUser(guildId, user.Id);
            if (userExists)
                // This is a bot and we've tracked it, bug in Discord API library can cause bots to not have the flag set correctly the first time we are notified of their connection.
                // If we've accidentally tracked this user thinking it was not a bot but it now is, remove it.
                await _cache.Users.RemoveUser(guildId, user.Id);
        }

        return continuationAllowed;
    }
}