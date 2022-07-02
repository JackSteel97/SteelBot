using DSharpPlus.Entities;
using SteelBot.Database.Models;
using SteelBot.DataProviders.SubProviders;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;

namespace SteelBot.Services;

public class LevelMessageSender
{
    private readonly GuildsProvider _guildsProvider;
    private readonly UsersProvider _usersProvider;
    private readonly ErrorHandlingService _errorHandlingService;

    public LevelMessageSender(GuildsProvider guildsProvider, UsersProvider usersProvider, ErrorHandlingService errorHandlingService)
    {
        _guildsProvider = guildsProvider;
        _usersProvider = usersProvider;
        _errorHandlingService = errorHandlingService;
    }

    public void SendLevelUpMessage(DiscordGuild discordGuild, DiscordUser discordUser)
    {
        if (_guildsProvider.TryGetGuild(discordGuild.Id, out var guild) && _usersProvider.TryGetUser(discordGuild.Id, discordUser.Id, out var user))
        {
            var channel = guild.GetLevelAnnouncementChannel(discordGuild);

            if (channel != null)
            {
                channel.SendMessageAsync(embed: EmbedGenerator.Info($"{discordUser.Mention} just advanced to level {user.CurrentLevel}!", "LEVEL UP!", $"Use {guild.CommandPrefix}Stats Me to check your progress"))
                    .FireAndForget(_errorHandlingService);
            }
        }
    }

    public void SendRankChangeDueToDeletionMessage(DiscordGuild discordGuild, ulong userId, RankRole previousRole, ulong? newRoleId = null)
    {
        if (_guildsProvider.TryGetGuild(discordGuild.Id, out var guild))
        {
            var channel = guild.GetLevelAnnouncementChannel(discordGuild);

            if (channel != null)
            {
                string newRoleText = newRoleId.HasValue && newRoleId.Value != default
                    ? $"your new role is **{newRoleId.Value.ToRoleMention()}**"
                    : "there are no rank roles eligible to replace it.";

                channel.SendMessageAsync(userId.ToUserMention(), embed: EmbedGenerator.Info($"Your previous rank role **{previousRole.RoleName}** has been deleted by an admin, {newRoleText}", "Rank Role Changed"))
                    .FireAndForget(_errorHandlingService);
            }
        }
    }

    public void SendRankGrantedMessage(DiscordGuild discordGuild, ulong userId, RankRole achievedRole, string roleMention)
    {
        if (_guildsProvider.TryGetGuild(discordGuild.Id, out var guild))
        {
            var channel = guild.GetLevelAnnouncementChannel(discordGuild);
            if (channel != null)
            {
                channel.SendMessageAsync(userId.ToUserMention(), embed: EmbedGenerator.Info($"You have been granted the **{roleMention}** role for reaching rank **{achievedRole.LevelRequired}**!", "Rank Role Granted!"))
                    .FireAndForget(_errorHandlingService);
            }
        }
    }
}