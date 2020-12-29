using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SteelBot.Database.Models
{
    public class Guild
    {
        public long RowId { get; set; }

        public ulong DiscordId { get; set; }

        public DateTime BotAddedTo { get; set; }

        public List<User> UsersInGuild { get; set; }
        public List<SelfRole> SelfRoles { get; set; }
        public List<RankRole> RankRoles { get; set; }

        public List<Trigger> Triggers { get; set; }

        [MaxLength(20)]
        public string CommandPrefix { get; set; }

        public ulong? LevelAnnouncementChannelId { get; set; }

        public int GoodBotVotes { get; set; }
        public int BadBotVotes { get; set; }

        /// <summary>
        /// Empty constructor.
        /// Do not remove - used by EF.
        /// </summary>
        public Guild() { }

        public Guild(ulong discordId)
        {
            DiscordId = discordId;
            BotAddedTo = DateTime.UtcNow;
        }

        public Guild Clone()
        {
            Guild guildCopy = (Guild)this.MemberwiseClone();
            return guildCopy;
        }
    }
}