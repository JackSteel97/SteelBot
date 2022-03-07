using SteelBot.DiscordModules.Pets.Enums;
using System;

namespace SteelBot.Database.Models.Pets
{
    public class PetBonus : ICloneable
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
        public object Clone()
        {
            var clone = (PetBonus)MemberwiseClone();
            clone.Pet = null;
            return clone;
        }
    }
}
