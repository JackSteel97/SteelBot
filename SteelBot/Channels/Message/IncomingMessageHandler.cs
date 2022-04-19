using Microsoft.Extensions.Logging;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DiscordModules.Pets;
using SteelBot.DiscordModules.RankRoles;
using SteelBot.DiscordModules.Triggers;
using SteelBot.Helpers.Levelling;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.Channels.Message
{
    public class IncomingMessageHandler
    {
        private readonly UsersProvider _usersProvider;
        private readonly ILogger<IncomingMessageHandler> _logger;
        private readonly LevelMessageSender _levelMessageSender;
        private readonly PetsDataHelper _petsDataHelper;
        private readonly RankRoleDataHelper _rankRoleDataHelper;
        private readonly TriggerDataHelper _triggerDataHelper;

        public IncomingMessageHandler(UsersProvider usersProvider,
            ILogger<IncomingMessageHandler> logger,
            LevelMessageSender levelMessageSender,
            PetsDataHelper petsDataHelper,
            RankRoleDataHelper rankRoleDataHelper,
            TriggerDataHelper triggerDataHelper)
        {
            _usersProvider = usersProvider;
            _logger = logger;
            _levelMessageSender = levelMessageSender;
            _petsDataHelper = petsDataHelper;
            _rankRoleDataHelper = rankRoleDataHelper;
            _triggerDataHelper = triggerDataHelper;
        }

        public async Task HandleMessage(IncomingMessage messageArgs)
        {
            bool levelledUp = await UpdateMessageCounters(messageArgs);
            if (levelledUp)
            {
                await _rankRoleDataHelper.UserLevelledUp(messageArgs.Guild.Id, messageArgs.User.Id, messageArgs.Guild);
            }
            await _triggerDataHelper.HandleNewMessage(messageArgs.Guild.Id, messageArgs.Message.Channel, messageArgs.Message.Content);
        }

        private async ValueTask<bool> UpdateMessageCounters(IncomingMessage messageArgs)
        {
            bool levelIncreased = false;
            if(_usersProvider.TryGetUser(messageArgs.Guild.Id, messageArgs.User.Id, out var user))
            {
                _logger.LogInformation("Updating message counters for User {UserId} in Guild {GuildId}", messageArgs.User.Id, messageArgs.Guild.Id);

                // Clone user to avoid making change to cache till db change confirmed.
                var copyOfUser = user.Clone();
                var availablePets = _petsDataHelper.GetAvailablePets(messageArgs.Guild.Id, messageArgs.User.Id, out _);
                if (copyOfUser.NewMessage(messageArgs.Message.Content.Length, availablePets))
                {
                    // Xp has changed.
                    levelIncreased = copyOfUser.UpdateLevel();
                    await _petsDataHelper.PetXpUpdated(availablePets, messageArgs.Guild);
                }
                await _usersProvider.UpdateUser(messageArgs.Guild.Id, copyOfUser);

                if (levelIncreased)
                {
                    _levelMessageSender.SendLevelUpMessage(messageArgs.Guild, messageArgs.User);
                }
            }
            return levelIncreased;
        }
    }
}
