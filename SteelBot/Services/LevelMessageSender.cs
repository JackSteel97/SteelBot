using DSharpPlus.Entities;
using SteelBot.Database.Models;
using SteelBot.Database.Models.Pets;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;

namespace SteelBot.Services;

public class LevelMessageSender
{
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly GuildsProvider _guildsProvider;
    private readonly UsersProvider _usersProvider;

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
                channel.SendMessageAsync(EmbedGenerator.Info($"{discordUser.Mention} just advanced to level {user.CurrentLevel}!", "LEVEL UP!",
                        $"Use {guild.CommandPrefix}Stats Me to check your progress"))
                    .FireAndForget(_errorHandlingService);
        }
    }

    public void SendStreakMessage(DiscordGuild discordGuild, DiscordUser discordUser, int streakDays, ulong earnedXp)
    {
        if (_guildsProvider.TryGetGuild(discordGuild.Id, out var guild) && _usersProvider.TryGetUser(discordGuild.Id, discordUser.Id, out var user))
        {
            var channel = guild.GetLevelAnnouncementChannel(discordGuild);

            if (channel == null) return;

            string content = "";
            if (!user.OptedOutOfMentions) content = discordUser.Mention;
            channel.SendMessageAsync(content,
                    EmbedGenerator.Info($"{discordUser.Mention} has been active for `{streakDays}` {(streakDays > 1 ? "days" : "day")} and earned a bonus of `{earnedXp}` XP!", "Active Streak!",
                        $"Use {guild.CommandPrefix}Stats Me to check your progress"))
                .FireAndForget(_errorHandlingService);
        }
    }
    
    public void SendPetDiedMessage(DiscordGuild discordGuild, DiscordUser discordUser, Pet pet)
    {
        if (!_guildsProvider.TryGetGuild(discordGuild.Id, out var guild) || !_usersProvider.TryGetUser(discordGuild.Id, discordUser.Id, out var user)) return;
        var channel = guild.GetLevelAnnouncementChannel(discordGuild);

        if (channel == null) return;

        string content = "";
        if (!user.OptedOutOfMentions) content = discordUser.Mention;
        var message = PetMessages.GetPetDiedMessage(pet);
        message.WithContent(content);
        channel.SendMessageAsync(message).FireAndForget(_errorHandlingService);
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

                string content = userId.ToUserMention();
                string embedPrefix = "";
                if (_usersProvider.TryGetUser(guild.DiscordId, userId, out var user) && user.OptedOutOfMentions)
                {
                    content = "";
                    embedPrefix = $"{userId.ToUserMention()}\n";
                }

                channel.SendMessageAsync(content,
                        EmbedGenerator.Info($"{embedPrefix}Your previous rank role **{previousRole.RoleName}** has been deleted by an admin, {newRoleText}", "Rank Role Changed"))
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
                string content = userId.ToUserMention();
                string embedPrefix = "";
                if (_usersProvider.TryGetUser(guild.DiscordId, userId, out var user) && user.OptedOutOfMentions)
                {
                    content = "";
                    embedPrefix = $"{userId.ToUserMention()}\n";
                }

                channel.SendMessageAsync(content,
                        EmbedGenerator.Info($"{embedPrefix}You have been granted the **{roleMention}** role for reaching rank **{achievedRole.LevelRequired}**!", "Rank Role Granted!"))
                    .FireAndForget(_errorHandlingService);
            }
        }
    }
}