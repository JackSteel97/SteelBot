using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.DiscordModules.Pets.Services;
using SteelBot.Helpers.Sentry;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.Channels.Pets;

public class PetCommandsChannel : BaseChannel<PetCommandAction>
{
    private readonly PetBefriendingService _befriendingService;
    private readonly PetManagementService _managementService;
    private readonly PetTreatingService _treatingService;
    private readonly PetViewingService _viewingService;
    private readonly PetBonusViewingService _bonusViewingService;
    private readonly IHub _sentry;

    /// <inheritdoc />
    public PetCommandsChannel(PetBefriendingService befriendingService,
        PetManagementService managementService,
        PetTreatingService treatingService,
        PetViewingService viewingService,
        PetBonusViewingService bonusViewingService,
        IHub sentry,
        ILogger logger,
        ErrorHandlingService errorHandlingService,
        string channelLabel = "Pets") : base(logger, errorHandlingService, channelLabel)
    {
        _befriendingService = befriendingService;
        _managementService = managementService;
        _treatingService = treatingService;
        _viewingService = viewingService;
        _bonusViewingService = bonusViewingService;
        _sentry = sentry;
    }

    /// <inheritdoc />
    protected override async ValueTask HandleMessage(PetCommandAction message)
    {
        var transaction = _sentry.StartNewConfiguredTransaction("Pets", message.Action.ToString(), message.User, message.Guild);
        switch (message.Action)
        {
            case PetCommandActionType.Search:
                await _befriendingService.Search();
                break;
            case PetCommandActionType.ManageAll:
                await _managementService.Manage();
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
    };
}