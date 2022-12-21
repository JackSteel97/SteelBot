using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using SteelBot.Channels.RankRole;
using SteelBot.DiscordModules.AuditLog.Services;
using SteelBot.Helpers.Extensions;
using SteelBot.Responders;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.RankRoles;

[Group("RankRoles")]
[Aliases("rr")]
[Description("Rank role management commands")]
[RequireGuild]
[RequireUserPermissions(Permissions.ManageRoles)]
public class RankRoleCommands : TypingCommandModule
{
    private readonly CancellationService _cancellationService;
    private readonly RankRoleManagementChannel _rankRoleManagementChannel;
    private readonly ErrorHandlingService _errorHandlingService;

    public RankRoleCommands(CancellationService cancellationService, RankRoleManagementChannel rankRoleManagementChannel, ErrorHandlingService errorHandlingService, ILogger<RankRoleCommands> logger, AuditLogService auditLogService)
        : base(logger, auditLogService)
    {
        _cancellationService = cancellationService;
        _rankRoleManagementChannel = rankRoleManagementChannel;
        _errorHandlingService = errorHandlingService;
    }

    [GroupCommand]
    [Description("View the rank roles set up in this server.")]
    [Cooldown(1, 60, CooldownBucketType.Channel)]
    public async Task ViewRankRoles(CommandContext context)
    {
        var action = new RankRoleManagementAction(RankRoleManagementActionType.View, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild);
        await _rankRoleManagementChannel.Write(action, _cancellationService.Token);
    }

    [Command("Set")]
    [Aliases("Create", "srr")]
    [Description("Sets the given role as a rank role at the given level.")]
    [Cooldown(5, 60, CooldownBucketType.Guild)]
    public async Task SetRankRole(CommandContext context, int requiredRank, [RemainingText] string roleName)
    {
        var action = new RankRoleManagementAction(RankRoleManagementActionType.Create, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild, roleName, requiredRank);
        await _rankRoleManagementChannel.Write(action, _cancellationService.Token);
    }

    [Command("Set")]
    [Priority(10)]
    public async Task SetRankRole(CommandContext context, int requiredRank, DiscordRole role)
    {
        var action = new RankRoleManagementAction(RankRoleManagementActionType.Create, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild, role.Id, role.Name,
            requiredRank);
        await _rankRoleManagementChannel.Write(action, _cancellationService.Token);
    }

    [Command("Remove")]
    [Aliases("Delete", "rrr")]
    [Description("Removes the given role from the list of rank roles, users will no longer be granted the role when they reach the required level.")]
    [Cooldown(5, 60, CooldownBucketType.Guild)]
    public async Task RemoveRankRole(CommandContext context, [RemainingText] string roleName)
    {
        var action = new RankRoleManagementAction(RankRoleManagementActionType.Delete, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild, roleName);
        await _rankRoleManagementChannel.Write(action, _cancellationService.Token);
    }

    [Command("Remove")]
    [Priority(10)]
    public async Task RemoveRankRole(CommandContext context, DiscordRole role)
    {
        var action = new RankRoleManagementAction(RankRoleManagementActionType.Delete, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild, role.Id, role.Name);
        await _rankRoleManagementChannel.Write(action, _cancellationService.Token);
    }
}