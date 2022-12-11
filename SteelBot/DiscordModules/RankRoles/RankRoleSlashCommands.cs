using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.Logging;
using SteelBot.Channels.RankRole;
using SteelBot.Helpers.Extensions;
using SteelBot.Responders;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.RankRoles;

[SlashCommandGroup("RankRoles", "Rank role management commands")]
[SlashRequireGuild]
[SlashRequireUserPermissions(Permissions.ManageRoles)]
public class RankRoleSlashCommands : InstrumentedApplicationCommandModule
{
    private readonly CancellationService _cancellationService;
    private readonly RankRoleManagementChannel _rankRoleManagementChannel;
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly ILogger<RankRoleSlashCommands> _logger;

    /// <inheritdoc />
    public RankRoleSlashCommands(CancellationService cancellationService, RankRoleManagementChannel rankRoleManagementChannel, ErrorHandlingService errorHandlingService, ILogger<RankRoleSlashCommands> logger)
        : base(logger)
    {
        _cancellationService = cancellationService;
        _rankRoleManagementChannel = rankRoleManagementChannel;
        _errorHandlingService = errorHandlingService;
        _logger = logger;  
    }

    [SlashCommand("View", "View the rank roles set up in this server")]
    [SlashCooldown(1, 60, SlashCooldownBucketType.Channel)]
    public async Task ViewRankRoles(InteractionContext context)
    {
        var action = new RankRoleManagementAction(RankRoleManagementActionType.View, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild);
        await _rankRoleManagementChannel.Write(action, _cancellationService.Token);
    }

    [SlashCommand("Set", "Sets the given role as a rank role at the given level")]
    [SlashCooldown(5, 60, SlashCooldownBucketType.Guild)]
    public async Task SetRankRole(InteractionContext context,
        [Option("RequiredRank", "The level the user needs to reach to be granted this role")] long requiredRank,
        [Option("Role", "The role to assign once the user reaches the required level")] DiscordRole role)
    {
        var action = new RankRoleManagementAction(RankRoleManagementActionType.Create, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild, role.Id, role.Name,
            (int)requiredRank);
        await _rankRoleManagementChannel.Write(action, _cancellationService.Token);
    }

    [SlashCommand("Remove", "Removes the given role from the list of rank roles")]
    [SlashCooldown(5, 60, SlashCooldownBucketType.Guild)]
    public async Task RemoveRankRole(InteractionContext context, [Option("Role", "The role to remove as a rank role")]DiscordRole role)
    {
        var action = new RankRoleManagementAction(RankRoleManagementActionType.Delete, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild, role.Id, role.Name);
        await _rankRoleManagementChannel.Write(action, _cancellationService.Token);
    }
    
    [SlashCommand("RemoveByName", "Removes the given role from the list of rank roles. Use this one if the role has been deleted")]
    [SlashCooldown(5, 60, SlashCooldownBucketType.Guild)]
    public async Task RemoveRankRole(InteractionContext context, [Option("RoleName", "The name of the role to remove as a rank role")]string roleName)
    {
        var action = new RankRoleManagementAction(RankRoleManagementActionType.Delete, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild, roleName);
        await _rankRoleManagementChannel.Write(action, _cancellationService.Token);
    }
}