using Microsoft.Extensions.Logging;
using SteelBot.DiscordModules.RankRoles.Services;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.Channels.RankRole;
public class RankRoleManagementChannel : BaseChannel<RankRoleManagementAction>
{
    private readonly RankRoleCreationService _rankRoleCreationService;
    private readonly RankRoleDeletionService _rankRoleDeletionService;
    private readonly RankRoleViewingService _rankRoleViewingService;

    public RankRoleManagementChannel(ILogger<RankRoleManagementChannel> logger,
            ErrorHandlingService errorHandlingService,
            RankRoleCreationService rankRoleCreationService,
            RankRoleDeletionService rankRoleDeletionService) : base(logger, errorHandlingService, "Rank Role")
    {
        _rankRoleCreationService = rankRoleCreationService;
        _rankRoleDeletionService = rankRoleDeletionService;
    }

    protected override async ValueTask HandleMessage(RankRoleManagementAction message)
    {
        switch (message.Action)
        {
            case RankRoleManagementActionType.Create:
                await _rankRoleCreationService.Create(message);
                break;
            case RankRoleManagementActionType.Delete:
                await _rankRoleDeletionService.Delete(message);
                break;
            case RankRoleManagementActionType.View:
                _rankRoleViewingService.View(message);
                break;
        }
    }
}
