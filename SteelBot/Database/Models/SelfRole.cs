using System;
using System.ComponentModel.DataAnnotations;

namespace SteelBot.Database.Models
{
    public class SelfRole
    {
        public long RowId { get; set; }

        [MaxLength(255)]
        public string RoleName { get; set; }

        public DateTime CreatedAt { get; set; }

        [MaxLength(255)]
        public string Description { get; set; }

        public bool Hidden { get; set; }
        public long GuildRowId { get; set; }
        public Guild Guild { get; set; }

        /// <summary>
        /// Empty constructor.
        /// Used by EF do not remove.
        /// </summary>
        public SelfRole() { }

        public SelfRole(string roleName, long guildRowId, string description, bool hidden)
        {
            RoleName = roleName;
            CreatedAt = DateTime.UtcNow;
            GuildRowId = guildRowId;
            Description = description;
            Hidden = hidden;
        }
    }
}