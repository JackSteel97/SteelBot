using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.DiscordModules.Roles.Services;
using SteelBot.Helpers.Sentry;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.Channels.SelfRole;

public class SelfRoleManagementChannel : BaseChannel<SelfRoleManagementAction>
{
    private readonly SelfRoleCreationService _selfRoleCreationService;
    private readonly SelfRoleMembershipService _selfRoleMembershipService;
    private readonly IHub _sentry;

    public SelfRoleManagementChannel(ILogger<SelfRoleManagementChannel> logger,
        ErrorHandlingService errorHandlingService,
        SelfRoleCreationService selfRoleCreationService,
        SelfRoleMembershipService selfRoleMembershipService,
        IHub sentry) : base(logger, errorHandlingService, "Self Role")
    {
        _selfRoleCreationService = selfRoleCreationService;
        _selfRoleMembershipService = selfRoleMembershipService;
        _sentry = sentry;
    }

    protected override async ValueTask HandleMessage(SelfRoleManagementAction message)
    {
        var transaction = _sentry.StartNewConfiguredTransaction("Self Roles", message.Action.ToString(), message.Member, message.Member.Guild);
        switch (message.Action)
        {
            case SelfRoleActionType.Join:
                await _selfRoleMembershipService.Join(message);
                break;
            case SelfRoleActionType.JoinAll:
                await _selfRoleMembershipService.JoinAll(message);
                break;
            case SelfRoleActionType.Leave:
                await _selfRoleMembershipService.Leave(message);
                break;
            case SelfRoleActionType.Create:
                await _selfRoleCreationService.Create(message);
                break;
            case SelfRoleActionType.Delete:
                await _selfRoleCreationService.Remove(message);
                break;
        }

        transaction.Finish();
    }
}