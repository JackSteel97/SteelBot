using SteelBot.DiscordModules.Pets.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.Database.Models.Pets
{
    public class Pet
    {
        public long RowId { get; set; }
        public ulong OwnerDiscordId { get; set; }

        [MaxLength(255)]
        public string Name { get; set; }

        public int Priority { get; set; }
        public ulong EarnedXp { get; set; }
        public int CurrentLevel { get; set; }
        public DateTime BornAt { get; set; }
        public DateTime FoundAt { get; set; }
        public Species Species { get; set; }
        public Size Size { get; set; }
        public Rarity Rarity { get; set; }
    }
}
