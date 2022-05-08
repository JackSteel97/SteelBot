using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using System;
using System.Collections.Generic;

namespace SteelBot.DiscordModules.Pets.Models
{
    public class BonusTotals
    {
        public Dictionary<BonusType, PetBonus> Totals { get; init; } = new Dictionary<BonusType, PetBonus>();

        /// <summary>
        /// Empty constructor.
        /// </summary>
        public BonusTotals() { }

        /// <summary>
        /// Immediately add pet bonuses to the total.
        /// </summary>
        /// <param name="pet">Pet to calculate totals of.</param>
        public BonusTotals(Pet pet)
        {
            Add(pet);
        }

        public void Add(Pet pet)
        {
            foreach (var bonus in pet.Bonuses)
            {
                Add(bonus);
            }
        }

        public void Add(PetBonus bonus)
        {
            if(Totals.TryGetValue(bonus.BonusType, out var bonusTotal))
            {
                bool isRounded = bonus.BonusType.IsRounded();
                var value = bonus.Value;
                if (isRounded)
                {
                    value = Math.Round(bonus.Value);
                }
                bonusTotal.Value += value;
            }
            else
            {
                // Clone to prevent affecting the source bonus.
                Totals.Add(bonus.BonusType, bonus.Clone());
            }
        }
    }
}
