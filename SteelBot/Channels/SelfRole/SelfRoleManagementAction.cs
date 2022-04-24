using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public SelfRoleActionType Action { get; set; }
        public DiscordMember Member { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
    }
}
