using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using SteelBot.Channels.SelfRole;
using SteelBot.DiscordModules.AuditLog.Services;
using SteelBot.Helpers.Extensions;
using SteelBot.Responders;
using SteelBot.Services;
using System;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Roles;

[Group("SelfRoles")]
[Aliases("sr")]
[Description("Self Role commands.")]
[RequireGuild]
public class RolesCommands : TypingCommandModule
{
    private readonly DataHelpers _dataHelpers;
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly CancellationService _cancellationService;
    private readonly SelfRoleManagementChannel _selfRoleManagementChannel;

    public RolesCommands(DataHelpers dataHelper, ErrorHandlingService errorHandlingService, CancellationService cancellationService, SelfRoleManagementChannel selfRoleManagementChannel, ILogger<RolesCommands> logger, AuditLogService auditLogService)
        : base(logger, auditLogService)
    {
        _dataHelpers = dataHelper;
        _errorHandlingService = errorHandlingService;
        _cancellationService = cancellationService;
        _selfRoleManagementChannel = selfRoleManagementChannel;
    }

    [GroupCommand]
    [Description("Displays the available self roles on this server.")]
    [Cooldown(1, 60, CooldownBucketType.Channel)]
    public Task ViewSelfRoles(CommandContext context)
    {
        _dataHelpers.Roles.DisplayRoles(context.Guild, new MessageResponder(context, _errorHandlingService));
        return Task.CompletedTask;
    }

    [Command("Join")]
    [Aliases("j")]
    [Description("Joins the self role specified.")]
    [Cooldown(10, 60, CooldownBucketType.User)]
    public async Task JoinRole(CommandContext context, [RemainingText] string roleName)
    {
        var actionType = roleName.Equals("All", StringComparison.OrdinalIgnoreCase)
            ? SelfRoleActionType.JoinAll
            : SelfRoleActionType.Join;

        var action = new SelfRoleManagementAction(actionType, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild, roleName);
        await _selfRoleManagementChannel.Write(action, _cancellationService.Token);
    }

    [Command("Join")]
    [Priority(10)]
    public async Task JoinRole(CommandContext context, DiscordRole role)
    {
        var action = new SelfRoleManagementAction(SelfRoleActionType.Join, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild, role.Name);
        await _selfRoleManagementChannel.Write(action, _cancellationService.Token);
    }

    [Command("Leave")]
    [Aliases("l")]
    [Description("Leaves the self role specified.")]
    [Cooldown(10, 60, CooldownBucketType.User)]
    public async Task LeaveRole(CommandContext context, [RemainingText] string roleName)
    {
        var action = new SelfRoleManagementAction(SelfRoleActionType.Leave, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild, roleName);
        await _selfRoleManagementChannel.Write(action, _cancellationService.Token);
    }

    [Command("Leave")]
    [Priority(10)]
    public async Task LeaveRole(CommandContext context, DiscordRole role)
    {
        var action = new SelfRoleManagementAction(SelfRoleActionType.Leave, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild, role.Name);
        await _selfRoleManagementChannel.Write(action, _cancellationService.Token);
    }

    [Command("Set")]
    [Aliases("Create")]
    [Description("Sets the given role as a self role that users can join themselves.")]
    [RequireUserPermissions(Permissions.ManageRoles)]
    [Cooldown(10, 60, CooldownBucketType.Guild)]
    public async Task SetSelfRole(CommandContext context, string roleName, string description)
    {
        var action = new SelfRoleManagementAction(SelfRoleActionType.Create, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild, roleName, description);
        await _selfRoleManagementChannel.Write(action, _cancellationService.Token);
    }

    [Command("Set")]
    [Priority(10)]
    public async Task SetSelfRole(CommandContext context, DiscordRole role, string description)
    {
        var action = new SelfRoleManagementAction(SelfRoleActionType.Create, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild, role.Name, description);
        await _selfRoleManagementChannel.Write(action, _cancellationService.Token);
    }

    [Command("Remove")]
    [Aliases("Delete")]
    [Description("Removes the given role from the list of self roles, users will no longer be able to join the role themselves.")]
    [RequireUserPermissions(Permissions.ManageRoles)]
    [Cooldown(10, 60, CooldownBucketType.Guild)]
    public async Task RemoveSelfRole(CommandContext context, [RemainingText] string roleName)
    {
        var action = new SelfRoleManagementAction(SelfRoleActionType.Delete, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild, roleName);
        await _selfRoleManagementChannel.Write(action, _cancellationService.Token);
    }

    [Command("Remove")]
    [Priority(10)]
    public async Task RemoveSelfRole(CommandContext context, DiscordRole role)
    {
        var action = new SelfRoleManagementAction(SelfRoleActionType.Delete, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild, role.Name);
        await _selfRoleManagementChannel.Write(action, _cancellationService.Token);
    }
}