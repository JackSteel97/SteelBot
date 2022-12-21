using DSharpPlus;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.Logging;
using SteelBot.Channels.SelfRole;
using SteelBot.DiscordModules.AuditLog.Services;
using SteelBot.Helpers.Extensions;
using SteelBot.Responders;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Roles;

[SlashCommandGroup("SelfRoles", "Self Role commands")]
[SlashRequireGuild]
public class RolesSlashCommands : InstrumentedApplicationCommandModule
{
    private readonly CancellationService _cancellationService;
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly ILogger<RolesSlashCommands> _logger;
    private readonly RolesDataHelper _rolesDataHelper;
    private readonly SelfRoleManagementChannel _selfRoleManagementChannel;

    /// <inheritdoc />
    public RolesSlashCommands(RolesDataHelper rolesDataHelper,
        ErrorHandlingService errorHandlingService,
        CancellationService cancellationService,
        SelfRoleManagementChannel selfRoleManagementChannel,
        ILogger<RolesSlashCommands> logger,
        AuditLogService auditLogService)
        : base(logger, auditLogService)
    {
        _rolesDataHelper = rolesDataHelper;
        _errorHandlingService = errorHandlingService;
        _cancellationService = cancellationService;
        _selfRoleManagementChannel = selfRoleManagementChannel;
        _logger = logger;
    }

    [SlashCommand("View", "Displays the available self roles on this server")]
    [Cooldown(1, 60, CooldownBucketType.Channel)]
    public Task ViewSelfRoles(InteractionContext context)
    {
        _rolesDataHelper.DisplayRoles(context.Guild, new InteractionResponder(context, _errorHandlingService));
        return Task.CompletedTask;
    }

    [SlashCommand("JoinAll", "Join all available self roles")]
    [SlashCooldown(10, 60, SlashCooldownBucketType.User)]
    public async Task JoinAllRoles(InteractionContext context)
    {
        var action = new SelfRoleManagementAction(SelfRoleActionType.JoinAll, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild);
        await _selfRoleManagementChannel.Write(action, _cancellationService.Token);
    }

    [SlashCommand("Join", "Join the self role specified")]
    [SlashCooldown(10, 60, SlashCooldownBucketType.User)]
    public async Task JoinRole(InteractionContext context, [Option("Role", "The role to attempt to join - only works if it is a self role")] DiscordRole role)
    {
        var action = new SelfRoleManagementAction(SelfRoleActionType.Join, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild, role.Name);
        await _selfRoleManagementChannel.Write(action, _cancellationService.Token);
    }

    [SlashCommand("Leave", "Leave the role specified")]
    [SlashCooldown(10, 60, SlashCooldownBucketType.User)]
    public async Task LeaveRole(InteractionContext context, [Option("Role", "The role to leave - only works if it is a self role")] DiscordRole role)
    {
        var action = new SelfRoleManagementAction(SelfRoleActionType.Leave, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild, role.Name);
        await _selfRoleManagementChannel.Write(action, _cancellationService.Token);
    }

    [SlashCommand("Set", "Sets the given role as a self role that users can join themselves")]
    [SlashCooldown(10, 60, SlashCooldownBucketType.Guild)]
    [RequireUserPermissions(Permissions.ManageRoles)]
    public async Task SetSelfRole(InteractionContext context,
        [Option("Role", "The role that should be set as a self role")]
        DiscordRole role,
        [Option("Description", "A description for the purpose of this role")]
        string description)
    {
        var action = new SelfRoleManagementAction(SelfRoleActionType.Create, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild, role.Name, description);
        await _selfRoleManagementChannel.Write(action, _cancellationService.Token);
    }

    [SlashCommand("Remove", "Removes the given role from the list of self roles, users will not be able to join the role")]
    [SlashCooldown(10, 60, SlashCooldownBucketType.Guild)]
    [RequireUserPermissions(Permissions.ManageRoles)]
    public async Task RemoveSelfRole(InteractionContext context, [Option("Role", "The role that should be removed as a self role")] DiscordRole role)
    {
        var action = new SelfRoleManagementAction(SelfRoleActionType.Delete, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild, role.Name);
        await _selfRoleManagementChannel.Write(action, _cancellationService.Token);
    }

    [SlashCommand("RemoveByName", "Removes the given role from the list of self roles by name - use this if the role has been deleted")]
    [SlashCooldown(10, 60, SlashCooldownBucketType.Guild)]
    [RequireUserPermissions(Permissions.ManageRoles)]
    public async Task RemoveSelfRole(InteractionContext context, [Option("Role", "The role that should be removed as a self role")] string roleName)
    {
        var action = new SelfRoleManagementAction(SelfRoleActionType.Delete, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild, roleName);
        await _selfRoleManagementChannel.Write(action, _cancellationService.Token);
    }
}