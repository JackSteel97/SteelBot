using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using SteelBot.Channels.RankRole;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.RankRoles;

public class RankRoleDataHelper
{
    private readonly RankRoleManagementChannel _rankRoleManagementChannel;
    private readonly CancellationService _cancellationService;

    public RankRoleDataHelper(RankRoleManagementChannel rankRoleManagementChannel, CancellationService cancellationService)
    {
        _rankRoleManagementChannel = rankRoleManagementChannel;
        _cancellationService = cancellationService;
    }

    public ValueTask CreateRankRole(CommandContext context, string roleName, int requiredLevel)
    {
        var message = new RankRoleManagementAction(RankRoleManagementActionType.Create, context.Message, roleName, requiredLevel);
        return WriteAction(message);
    }

    public ValueTask CreateRankRole(CommandContext context, DiscordRole role, int requiredLevel)
    {
        var message = new RankRoleManagementAction(RankRoleManagementActionType.Create, context.Message, role.Id, role.Name, requiredLevel);
        return WriteAction(message);
    }

    public ValueTask DeleteRankRole(CommandContext context, string roleName)
    {
        var message = new RankRoleManagementAction(RankRoleManagementActionType.Delete, context.Message, roleName);
        return WriteAction(message);
    }

    public ValueTask DeleteRankRole(CommandContext context, DiscordRole role)
    {
        var message = new RankRoleManagementAction(RankRoleManagementActionType.Delete, context.Message, role.Id, role.Name);
        return WriteAction(message);
    }

    public ValueTask ViewRankRoles(CommandContext context)
    {
        var message = new RankRoleManagementAction(RankRoleManagementActionType.View, context.Message);
        return WriteAction(message);
    }

    private ValueTask WriteAction(RankRoleManagementAction action) => _rankRoleManagementChannel.Write(action, _cancellationService.Token);
}