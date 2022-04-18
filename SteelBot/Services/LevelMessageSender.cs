using DSharpPlus.Entities;
using SteelBot.DataProviders.SubProviders;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;

namespace SteelBot.Services
{
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
                DiscordChannel channel = guild.GetLevelAnnouncementChannel(discordGuild);

                if (channel != null)
                {
                    channel.SendMessageAsync(embed: EmbedGenerator.Info($"{discordUser.Mention} just advanced to level {user.CurrentLevel}!", "LEVEL UP!", $"Use {guild.CommandPrefix}Stats Me to check your progress"))
                        .FireAndForget(_errorHandlingService);
                }
            }
        }
    }
}
