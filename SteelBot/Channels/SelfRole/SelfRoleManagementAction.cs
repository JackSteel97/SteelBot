using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace SteelBot.Channels.SelfRole
{
    public enum SelfRoleActionType
    {
        Create,
        Delete,
        Join,
        Leave,
        JoinAll
    }

    public class SelfRoleManagementAction
    {
        public SelfRoleActionType Action { get; init; }
        public DiscordMember Member { get; init; }
        public DiscordMessage SourceMessage { get; init; }
        public string RoleName { get; init; }
        public string Description { get; init; }

        public SelfRoleManagementAction(SelfRoleActionType action, DiscordMember member, DiscordMessage sourceMessage, string roleName, string description)
        {
            Action = action;
            Member = member;
            SourceMessage = sourceMessage;
            RoleName = roleName;
            Description = description;
        }

        public SelfRoleManagementAction(SelfRoleActionType action, DiscordMember member, DiscordMessage sourceMessage, string roleName)
        {
            Action = action;
            Member = member;
            SourceMessage = sourceMessage;
            RoleName = roleName;
        }

        public SelfRoleManagementAction(SelfRoleActionType action, DiscordMember member, DiscordMessage sourceMessage)
        {
            Action = action;
            Member = member;
            SourceMessage = sourceMessage;
        }

        public Task<DiscordMessage> RespondAsync(DiscordMessageBuilder msg)
        {
            return SourceMessage.RespondAsync(msg);
        }
    }
}
