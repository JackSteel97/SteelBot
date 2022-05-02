using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using SteelBot.Channels.SelfRole;
using SteelBot.DiscordModules.Roles.Services;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Roles;

public class RolesDataHelper
{
    private readonly SelfRoleManagementChannel _selfRoleManagementChannel;
    private readonly SelfRoleViewingService _selfRoleViewingService;
    private readonly CancellationService _cancellationService;

    public RolesDataHelper(SelfRoleManagementChannel selfRoleManagementChannel,
        CancellationService cancellationService,
        SelfRoleViewingService selfRoleViewingService)
    {
        _selfRoleManagementChannel = selfRoleManagementChannel;
        _cancellationService = cancellationService;
        _selfRoleViewingService = selfRoleViewingService;
    }

    public Task CreateRole(CommandContext context, DiscordRole role, string description)
    {
        return CreateRole(context, role.Name, description);
    }

    public async Task CreateRole(CommandContext context, string roleName, string description)
    {
        var change = new SelfRoleManagementAction(SelfRoleActionType.Create, context.Member, context.Message, roleName, description);
        await _selfRoleManagementChannel.Write(change, _cancellationService.Token);
    }

    public Task RemoveRole(CommandContext context, DiscordRole role)
    {
        return RemoveRole(context, role.Name);
    }

    public async Task RemoveRole(CommandContext context, string roleName)
    {
        var change = new SelfRoleManagementAction(SelfRoleActionType.Delete, context.Member, context.Message, roleName);
        await _selfRoleManagementChannel.Write(change, _cancellationService.Token);
    }

    public Task JoinRole(CommandContext context, DiscordRole role)
    {
        return JoinRole(context, role.Name);
    }

    public async Task JoinRole(CommandContext context, string roleName)
    {
        var change = new SelfRoleManagementAction(SelfRoleActionType.Join, context.Member, context.Message, roleName);
        await _selfRoleManagementChannel.Write(change, _cancellationService.Token);
    }

    public Task LeaveRole(CommandContext context, DiscordRole role)
    {
        return LeaveRole(context, role.Name);
    }

    public async Task LeaveRole(CommandContext context, string roleName)
    {
        var change = new SelfRoleManagementAction(SelfRoleActionType.Leave, context.Member, context.Message, roleName);
        await _selfRoleManagementChannel.Write(change, _cancellationService.Token);
    }

    public async Task JoinAllRoles(CommandContext context)
    {
        var change = new SelfRoleManagementAction(SelfRoleActionType.JoinAll, context.Member, context.Message);
        await _selfRoleManagementChannel.Write(change, _cancellationService.Token);
    }

    public void DisplayRoles(CommandContext context)
    {
        _selfRoleViewingService.DisplaySelfRoles(context);
    }
}
