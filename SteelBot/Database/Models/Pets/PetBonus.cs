using SteelBot.DiscordModules.Pets.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.Database.Models.Pets
{
    public class PetBonus
    {
        public long RowId { get; set; }
        public long PetId { get; set; }
        public Pet Pet { get; set; }
        public BonusType BonusType { get; set; }
        public double PercentageValue { get; set; }
    }
}
