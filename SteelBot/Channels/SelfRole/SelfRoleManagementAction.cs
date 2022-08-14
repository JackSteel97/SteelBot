using DSharpPlus.Entities;
using SteelBot.Responders;
using System.Threading.Tasks;

namespace SteelBot.Channels.SelfRole;

public enum SelfRoleActionType
{
    Create,
    Delete,
    Join,
    Leave,
    JoinAll
}

public class SelfRoleManagementAction : BaseAction<SelfRoleActionType>
{
    public string RoleName { get; }
    public string Description { get; }

    public SelfRoleManagementAction(SelfRoleActionType action, IResponder responder, DiscordMember member, DiscordGuild guild, string roleName, string description)
    : base(action, responder, member, guild)
    {
        RoleName = roleName;
        Description = description;
    }

    public SelfRoleManagementAction(SelfRoleActionType action, IResponder responder, DiscordMember member, DiscordGuild guild, string roleName)
    : base(action, responder, member, guild)
    {
        RoleName = roleName;
    }
    public SelfRoleManagementAction(SelfRoleActionType action, IResponder responder, DiscordMember member, DiscordGuild guild)
    :base(action, responder, member, guild) {}
}