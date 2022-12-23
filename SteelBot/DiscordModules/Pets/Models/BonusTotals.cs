using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using System;
using System.Collections.Generic;

namespace SteelBot.DiscordModules.Pets.Models;

public class BonusTotals
{
    public Dictionary<BonusType, PetBonus> Totals { get; init; } = new();

    /// <summary>
    ///     Empty constructor.
    /// </summary>
    public BonusTotals() { }

    /// <summary>
    ///     Immediately add pet bonuses to the total.
    /// </summary>
    /// <param name="pet">Pet to calculate totals of.</param>
    public BonusTotals(PetWithActivation pet)
    {
        Add(pet);
    }

    public BonusTotals(Pet pet)
    {
        Add(new PetWithActivation(pet, true));
    }

    public void Add(PetWithActivation petWithActivation)
    {
        if (petWithActivation.Active)
        {
            foreach (var bonus in petWithActivation.Pet.Bonuses) Add(bonus);
        }
        else
        {
            var slotsBonus = petWithActivation.Pet.Bonuses.Find(b => b.BonusType == BonusType.PetSlots);
            if (slotsBonus != default) Add(slotsBonus);
        }
    }

    public void Add(PetBonus bonus)
    {
        if (Totals.TryGetValue(bonus.BonusType, out var bonusTotal))
        {
            bool isRounded = bonus.BonusType.IsRounded();
            double value = bonus.Value;
            if (isRounded) value = Math.Round(bonus.Value);

            bonusTotal.Value += value;
        }
        else
        {
            // Clone to prevent affecting the source bonus.
            Totals.Add(bonus.BonusType, bonus.Clone());
        }
    }
}