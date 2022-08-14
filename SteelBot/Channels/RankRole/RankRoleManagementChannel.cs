using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.DiscordModules.RankRoles.Services;
using SteelBot.Helpers.Sentry;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.Channels.RankRole;
public class RankRoleManagementChannel : BaseChannel<RankRoleManagementAction>
{
    private readonly RankRoleCreationService _rankRoleCreationService;
    private readonly RankRoleDeletionService _rankRoleDeletionService;
    private readonly RankRoleViewingService _rankRoleViewingService;
    private readonly IHub _sentry;

    public RankRoleManagementChannel(ILogger<RankRoleManagementChannel> logger,
            ErrorHandlingService errorHandlingService,
            RankRoleCreationService rankRoleCreationService,
            RankRoleDeletionService rankRoleDeletionService,
            RankRoleViewingService rankRoleViewingService,
            IHub sentry) : base(logger, errorHandlingService, "Rank Role")
    {
        _rankRoleCreationService = rankRoleCreationService;
        _rankRoleDeletionService = rankRoleDeletionService;
        _rankRoleViewingService = rankRoleViewingService;
        _sentry = sentry;
    }

    protected override async ValueTask HandleMessage(RankRoleManagementAction message)
    {
        var transaction = _sentry.StartNewConfiguredTransaction("Rank Roles", message.Action.ToString(), message.Member, message.Guild);
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

        transaction.Finish();
    }
}