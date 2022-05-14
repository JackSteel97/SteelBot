using Humanizer;
using SteelBot.DiscordModules.Pets.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;

namespace SteelBot.Database.Models.Pets
{
    [DebuggerDisplay("{Rarity} {Species} {Name}")]
    public class Pet
    {
        public long RowId { get; set; }
        public ulong OwnerDiscordId { get; set; }
        [MaxLength(70)]
        public string Name { get; set; }

        public int Priority { get; set; }
        public double EarnedXp { get; set; }
        public int CurrentLevel { get; set; } = 1;
        public DateTime BornAt { get; set; }
        public DateTime FoundAt { get; set; }
        public Species Species { get; set; }
        public Size Size { get; set; }
        public Rarity Rarity { get; set; }
        public bool IsCorrupt { get; set; }
        public List<PetAttribute> Attributes { get; set; }
        public List<PetBonus> Bonuses { get; set; }

        public override string ToString()
        {
            TimeSpan age = DateTime.UtcNow - BornAt;

            return $"A {Rarity} {age.Humanize(maxUnit: Humanizer.Localisation.TimeUnit.Year)} old, {Size} {Species.GetName()} with\n\t{string.Join("\n\t", Attributes.Select(a => $"{a.Description} {a.Name}"))}";
        }

        public string GetName()
        {
            return Name?.Trim() ?? $"Unnamed {Species.GetName()}";
        }

        public void AddBonuses(List<PetBonus> bonuses)
        {
            foreach (var bonus in bonuses)
            {
                AddBonus(bonus);
            }
        }

        public void AddBonus(PetBonus bonus)
        {
            if (Bonuses == default)
            {
                Bonuses = new List<PetBonus>(1);
            }

            var existingBonusOfThisType = Bonuses.Find(b => b.BonusType == bonus.BonusType);
            if (existingBonusOfThisType != null)
            {
                existingBonusOfThisType.Value += bonus.Value;
            }
            else
            {
                Bonuses.Add(bonus);
            }
        }

        public bool IsPrimary => Priority == 0;

        /// <summary>
        /// Deep clone.
        /// </summary>
        /// <returns>A deep clone of this object.</returns>
        public Pet Clone()
        {
            var clone = (Pet)MemberwiseClone();
            clone.Attributes = Attributes.ConvertAll(x => x.Clone());
            clone.Bonuses = Bonuses.ConvertAll(x => x.Clone());

            return clone;
        }
    }
}
