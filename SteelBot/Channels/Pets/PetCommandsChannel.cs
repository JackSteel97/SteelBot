using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.DiscordModules.Pets.Services;
using SteelBot.Helpers.Extensions;
using SteelBot.Helpers.Sentry;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.Channels.Pets;

public class PetCommandsChannel : BaseChannel<PetCommandAction>
{
    private readonly PetSearchingService _searchingService;
    private readonly PetManagementService _managementService;
    private readonly PetTreatingService _treatingService;
    private readonly PetViewingService _viewingService;
    private readonly PetBonusViewingService _bonusViewingService;
    private readonly IHub _sentry;

    /// <inheritdoc />
    public PetCommandsChannel(PetSearchingService searchingService,
        PetManagementService managementService,
        PetTreatingService treatingService,
        PetViewingService viewingService,
        PetBonusViewingService bonusViewingService,
        IHub sentry,
        ILogger<PetCommandsChannel> logger,
        ErrorHandlingService errorHandlingService,
        string channelLabel = "Pets") : base(logger, errorHandlingService, channelLabel)
    {
        _searchingService = searchingService;
        _managementService = managementService;
        _treatingService = treatingService;
        _viewingService = viewingService;
        _bonusViewingService = bonusViewingService;
        _sentry = sentry;
    }

    /// <inheritdoc />
    protected override ValueTask HandleMessage(PetCommandAction message)
    {
        Task.Run(async () =>
        {
            var transaction = _sentry.StartNewConfiguredTransaction("Pets", message.Action.ToString(), message.Member, message.Guild);
            switch (message.Action)
            {
                case PetCommandActionType.Search:
                    await _searchingService.Search(message);
                    break;
                case PetCommandActionType.ManageOne:
                    await _managementService.ManagePet(message, message.PetId, transaction);
                    break;
                case PetCommandActionType.ManageAll:
                    await _managementService.Manage(message);
                    break;
                case PetCommandActionType.Treat:
                    await _treatingService.Treat(message);
                    break;
                case PetCommandActionType.View:
                    _viewingService.View(message);
                    break;
                case PetCommandActionType.ViewBonuses:
                    _bonusViewingService.View(message);
                    break;
            }

            transaction.Finish();
        }).FireAndForget(_errorHandlingService);
        
        return ValueTask.CompletedTask;
    }
}