using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DiscordModules.Pets;
using SteelBot.DiscordModules.RankRoles;
using SteelBot.DiscordModules.RankRoles.Helpers;
using SteelBot.DiscordModules.Triggers;
using SteelBot.Helpers.Levelling;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.Channels.Message
{
    public class IncomingMessageHandler
    {
        private readonly UsersProvider _usersProvider;
        private readonly ILogger<IncomingMessageHandler> _logger;
        private readonly LevelMessageSender _levelMessageSender;
        private readonly PetsDataHelper _petsDataHelper;
        private readonly TriggerDataHelper _triggerDataHelper;
        private readonly RankRolesProvider _rankRolesProvider;

        public IncomingMessageHandler(UsersProvider usersProvider,
            ILogger<IncomingMessageHandler> logger,
            LevelMessageSender levelMessageSender,
            PetsDataHelper petsDataHelper,
            TriggerDataHelper triggerDataHelper,
            RankRolesProvider rankRolesProvider)
        {
            _usersProvider = usersProvider;
            _logger = logger;
            _levelMessageSender = levelMessageSender;
            _petsDataHelper = petsDataHelper;
            _triggerDataHelper = triggerDataHelper;
            _rankRolesProvider = rankRolesProvider;
        }

        public async Task HandleMessage(IncomingMessage messageArgs, ISpan transaction)
        {
            var messageCountersSpan = transaction.StartChild("Update Message Counters");
            bool levelledUp = await UpdateMessageCounters(messageArgs, messageCountersSpan);
            messageCountersSpan.Finish();

            if (levelledUp)
            {
                var levelUpSpan = transaction.StartChild("Rank Role User Levelled Up", "From Message Xp");
                await RankRoleShared.UserLevelledUp(messageArgs.Guild.Id, messageArgs.User.Id, messageArgs.Guild, _rankRolesProvider, _usersProvider, _levelMessageSender);
                levelUpSpan.Finish();
            }

            var triggersSpan = transaction.StartChild("Triggers Handle Message");
            await _triggerDataHelper.HandleNewMessage(messageArgs.Guild.Id, messageArgs.Message.Channel, messageArgs.Message.Content);
            triggersSpan.Finish();
        }

        private async ValueTask<bool> UpdateMessageCounters(IncomingMessage messageArgs, ISpan transaction)
        {
            bool levelIncreased = false;
            if(_usersProvider.TryGetUser(messageArgs.Guild.Id, messageArgs.User.Id, out var user))
            {
                _logger.LogInformation("Updating message counters for User {UserId} in Guild {GuildId}", messageArgs.User.Id, messageArgs.Guild.Id);

                // Clone user to avoid making change to cache till db change confirmed.
                var copyOfUser = user.Clone();
                var getPetsSpan = transaction.StartChild("Get Available Pets");
                var availablePets = _petsDataHelper.GetAvailablePets(messageArgs.Guild.Id, messageArgs.User.Id, out _);
                getPetsSpan.Finish();

                var xpUpdateSpan = transaction.StartChild("Update User Xp");
                if (copyOfUser.NewMessage(messageArgs.Message.Content.Length, availablePets))
                {
                    // Xp has changed.
                    var userLevelSpan = xpUpdateSpan.StartChild("Update User Level");
                    levelIncreased = copyOfUser.UpdateLevel();
                    userLevelSpan.Finish();

                    var petLevelSpan = xpUpdateSpan.StartChild("Update Pets Levels");
                    await _petsDataHelper.PetXpUpdated(availablePets, messageArgs.Guild, copyOfUser.CurrentLevel);
                    petLevelSpan.Finish();
                }
                xpUpdateSpan.Finish();
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
