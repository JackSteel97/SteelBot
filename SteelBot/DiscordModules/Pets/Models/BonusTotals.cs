using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using System.Collections.Generic;

namespace SteelBot.DiscordModules.Pets.Models
{
    public class BonusTotals
    {
        public Dictionary<BonusType, PetBonus> Totals { get; init; } = new Dictionary<BonusType, PetBonus>();
        public double PassiveOffline { get; private set; }

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

            if (pet.Rarity == Rarity.Legendary)
            {
                AddPassive(pet.CurrentLevel);
            }
            else if (pet.Rarity == Rarity.Mythical)
            {
                AddPassive(pet.CurrentLevel * 2);
            }
        }

        public void Add(PetBonus bonus)
        {
            if(Totals.TryGetValue(bonus.BonusType, out var bonusTotal))
            {
                bonusTotal.Value += bonus.Value;
            }
            else
            {
                // Clone to prevent affecting the source bonus.
                Totals.Add(bonus.BonusType, bonus.Clone());
            }
        }

        public void AddPassive(double passiveXp)
        {
            PassiveOffline += passiveXp;
        }

    }
}
