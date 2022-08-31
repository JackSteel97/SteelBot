using DSharpPlus.Entities;
using SteelBot.Responders;
using System.Threading.Tasks;

namespace SteelBot.Channels.RankRole;

public enum RankRoleManagementActionType
{
    Create,
    Delete,
    View
}

public record RankRoleManagementAction : BaseAction<RankRoleManagementActionType>
{
    public string RoleName { get; }
    public ulong RoleId { get; }
    public int RequiredRank { get; }

    public RankRoleManagementAction(RankRoleManagementActionType action, IResponder responder, DiscordMember member, DiscordGuild guild, string roleName, int requiredRank = default)
    : base (action, responder, member, guild)
    {
        RoleName = roleName;
        RequiredRank = requiredRank;
    }

    public RankRoleManagementAction(RankRoleManagementActionType action, IResponder responder, DiscordMember member, DiscordGuild guild,ulong roleId, string roleName, int requiredRank = default)
    :base(action, responder, member, guild)
    {
        RoleId = roleId;
        RoleName = roleName;
        RequiredRank = requiredRank;
    }

    public RankRoleManagementAction(RankRoleManagementActionType action, IResponder responder, DiscordMember member, DiscordGuild guild)
    :base(action, responder, member, guild)
    {}

    public string GetRoleIdentifier() => RoleId == default ? RoleName : RoleId.ToString();
}