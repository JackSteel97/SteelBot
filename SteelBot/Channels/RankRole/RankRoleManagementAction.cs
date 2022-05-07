using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace SteelBot.Channels.RankRole;

public enum RankRoleManagementActionType
{
    Create,
    Delete,
    View
}

public class RankRoleManagementAction
{
    public RankRoleManagementActionType Action { get; init; }
    public DiscordMessage SourceMessage { get; init; }
    public string RoleName { get; init; }
    public ulong RoleId { get; init; }
    public int RequiredRank { get; init; }
    public DiscordGuild Guild => SourceMessage.Channel.Guild;

    public RankRoleManagementAction(RankRoleManagementActionType action, DiscordMessage sourceMessage, string roleName, int requiredRank = default)
    {
        Action = action;
        SourceMessage = sourceMessage;
        RoleName = roleName;
        RequiredRank = requiredRank;
    }

    public RankRoleManagementAction(RankRoleManagementActionType action, DiscordMessage sourceMessage, ulong roleId, string roleName, int requiredRank = default)
    {
        Action = action;
        SourceMessage = sourceMessage;
        RoleId = roleId;
        RoleName = roleName;
        RequiredRank = requiredRank;
    }

    public RankRoleManagementAction(RankRoleManagementActionType action, DiscordMessage sourceMessage)
    {
        Action = action;
        SourceMessage = sourceMessage;
    }

    public Task<DiscordMessage> RespondAsync(DiscordMessageBuilder msg) => SourceMessage.RespondAsync(msg);

    public string GetRoleIdentifier() => RoleId == default ? RoleName : RoleId.ToString();
}
