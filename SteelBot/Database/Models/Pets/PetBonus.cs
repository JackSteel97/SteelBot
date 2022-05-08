using SteelBot.DiscordModules.Pets.Enums;
using System;
using System.Diagnostics;

namespace SteelBot.Database.Models.Pets
{
    [DebuggerDisplay("{BonusType} : {Value}")]
    public class PetBonus
    {
        public long RowId { get; set; }
        public long PetId { get; set; }
        public Pet Pet { get; set; }
        public BonusType BonusType { get; set; }
        public double Value { get; set; }

        /// <summary>
        /// Shallow clones the bonus, <see cref="Pet"/> is nullified during this to prevent accidental alterations.
        /// </summary>
        /// <returns>A clone of the bonus</returns>
        public PetBonus Clone()
        {
            var clone = (PetBonus)MemberwiseClone();
            clone.Pet = null;
            return clone;
        }
    }
}
