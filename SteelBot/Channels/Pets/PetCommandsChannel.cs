using Microsoft.Extensions.Logging;
using SteelBot.DiscordModules.Pets.Services;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.Channels.Pets;

public class PetCommandsChannel : BaseChannel<PetCommandAction>
{
    private readonly PetBonusViewingService _bonusViewingService;
    private readonly PetManagementService _managementService;
    private readonly PetSearchingService _searchingService;
    private readonly PetTreatingService _treatingService;
    private readonly PetViewingService _viewingService;
    private readonly PetDeathService _petDeathService;

    /// <inheritdoc />
    public PetCommandsChannel(PetSearchingService searchingService,
        PetManagementService managementService,
        PetTreatingService treatingService,
        PetViewingService viewingService,
        PetBonusViewingService bonusViewingService,
        ILogger<PetCommandsChannel> logger,
        ErrorHandlingService errorHandlingService,
        PetDeathService petDeathService,
        string channelLabel = "Pets") : base(logger, errorHandlingService, channelLabel)
    {
        _searchingService = searchingService;
        _managementService = managementService;
        _treatingService = treatingService;
        _viewingService = viewingService;
        _bonusViewingService = bonusViewingService;
        _petDeathService = petDeathService;
    }

    /// <inheritdoc />
    protected override ValueTask HandleMessage(PetCommandAction message)
    {
        Task.Run(async () =>
        {
            switch (message.Action)
            {
                case PetCommandActionType.Search:
                    await _searchingService.Search(message);
                    break;
                case PetCommandActionType.ManageOne:
                    await _managementService.ManagePet(message, message.PetId);
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
                case PetCommandActionType.CheckForDeath:
                    await _petDeathService.RunCheck(message);
                    break;
            }
        }).FireAndForget(ErrorHandlingService);

        return ValueTask.CompletedTask;
    }
}