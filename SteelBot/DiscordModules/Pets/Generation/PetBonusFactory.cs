using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace SteelBot.DiscordModules.Pets.Generation;
public static class PetBonusFactory
{
    private static readonly BonusType[] _excludedTypes = new BonusType[] { BonusType.None, BonusType.PetSlots, BonusType.OfflineXp };

    public static List<PetBonus> GenerateMany(Pet pet, int levelOfUser)
    {
        int maxBonuses = pet.Rarity.GetStartingBonusCount();
        var bonuses = new List<PetBonus>(maxBonuses);

        for (int i = 0; i < maxBonuses; ++i)
        {
            var bonus = Generate(pet, levelOfUser, bonuses);
            bonuses.Add(bonus);
        }
        return bonuses;
    }

    public static PetBonus Generate(Pet pet, int levelOfUser, List<PetBonus> existingBonuses = default)
    {
        existingBonuses ??= pet.Bonuses;
        bool validBonus = true;
        double maxPercentageBonus = pet.Rarity.GetMaxBonusValue();

        var bonus = new PetBonus()
        {
            Pet = pet,
        };
        do
        {
            bonus.BonusType = GetWeightedRandomBonusType(pet.Rarity, levelOfUser);

            if (bonus.BonusType.IsPercentage())
            {
                validBonus = HandlePercentageBonusGeneration(pet, maxPercentageBonus, bonus, existingBonuses, pet.CurrentLevel, levelOfUser);
            }
            else if (bonus.BonusType == BonusType.OfflineXp)
            {
                HandleOfflineXpBonusGeneration(bonus, pet.Rarity, pet.CurrentLevel, levelOfUser);
            }
            else
            {
                HandleIntegerBonusGeneration(bonus, pet.Rarity);
            }
        } while (!validBonus);

        if (pet.IsCorrupt)
        {
            bonus.Value = ScaleCorruptBonus(bonus.Value, pet.Rarity);
        }
        return bonus;
    }

    public static Pet Corrupt(Pet pet, int levelOfUser)
    {
        double totalBonusValue = 0;
        // Flip existing bonuses
        foreach (var bonus in pet.Bonuses)
        {
            bonus.Value = ScaleCorruptBonus(bonus.Value, pet.Rarity);

            if (bonus.BonusType.IsPercentage())
            {
                totalBonusValue += Math.Abs(bonus.Value);
            }
            else
            {
                totalBonusValue += Math.Abs(bonus.Value) / 100;
            }

            bool isNegative = bonus.BonusType.IsNegative();
            if ((bonus.Value < 0 && isNegative)         // Negative bonus type that is providing a positive effect.
                || (bonus.Value > 0 && !isNegative))    // Positive bonus type that is providing a positive effect
            {
                // Invert bonus - i.e. make it a negative effect.
                bonus.Value *= -1;
            }
        }

        // Generate single large positive bonus.
        var newBonus = Generate(pet, levelOfUser, pet.Bonuses);
        bool newBonusIsNegative = newBonus.BonusType.IsNegative();
        if ((newBonus.Value > 0 && newBonusIsNegative)
            || (newBonus.Value < 0 && !newBonusIsNegative))
        {
            // Bonus is providing a negative effect, invert it to provide a positive effect.
            newBonus.Value *= -1;
        }

        if (!newBonus.BonusType.IsPercentage())
        {
            // Make the total none-percentage scaled.
            totalBonusValue *= 100;
        }

        if (newBonus.Value < 0)
        {
            // Bonus is a negative value with positive effect, get new value up to max -1 limit.
            newBonus.Value = GetRandomPercentageBonus(maxValue: newBonus.Value, minValue: -1);
        }
        else if (totalBonusValue > newBonus.Value)
        {
            // Sum of bonuses is higher than the generated bonus, get a new value between these limits.
            newBonus.Value = GetRandomPercentageBonus(totalBonusValue, newBonus.Value);
        }
        pet.IsCorrupt = true;

        newBonus.Value = ScaleCorruptBonus(newBonus.Value, pet.Rarity);
        pet.AddBonus(newBonus);
        return pet;
    }

    private static BonusType GetWeightedRandomBonusType(Rarity rarity, int userLevel)
    {
        double rarityValue = (double)rarity;
        double chanceToGeneratePassiveXp = rarityValue / 60;
        double chanceToGeneratePetSlots = rarityValue / 50;

        return userLevel switch
        {
            > 20 when MathsHelper.TrueWithProbability(chanceToGeneratePetSlots) => BonusType.PetSlots,
            > 50 when MathsHelper.TrueWithProbability(chanceToGeneratePassiveXp) => BonusType.OfflineXp,
            _ => PetGenerationShared.GetRandomEnumValue(_excludedTypes),
        };
    }

    private static void HandleOfflineXpBonusGeneration(PetBonus bonus, Rarity rarity, int petLevel, int userLevel)
    {
        double baseValue = (double)rarity;

        double chanceToGetMore = baseValue / 10;

        bonus.Value = ApplyScaling(baseValue, petLevel, userLevel);

        while (MathsHelper.TrueWithProbability(chanceToGetMore))
        {
            bonus.Value += baseValue;
        }
    }

    private static double ApplyScaling(double bonusValue, int petLevel, int userLevel)
    {
        double petLevelMultiplier = 1 + ((double)petLevel / 50);
        double userLevelMultiplier = 1 + ((double)userLevel / 100);

        bonusValue *= petLevelMultiplier;
        bonusValue *= userLevelMultiplier;
        return bonusValue;
    }

    private static void HandleIntegerBonusGeneration(PetBonus bonus, Rarity rarity)
    {
        bonus.Value = 1;
        while (MathsHelper.TrueWithProbability(0.1))
        {
            ++bonus.Value;
        }

        double probabilityToGoNegative = ((double)rarity + 1) / 10;
        if (MathsHelper.TrueWithProbability(probabilityToGoNegative))
        {
            bonus.Value *= -1;
        }
    }

    private static bool HandlePercentageBonusGeneration(Pet pet, double maxBonus, PetBonus bonus, List<PetBonus> existingBonuses, int petLevel, int userLevel)
    {
        bool validBonus = true;
        double minBonus = pet.Rarity < Rarity.Rare && !bonus.BonusType.IsNegative() ? 0 : maxBonus * -1; // Lower rarities shouldn't have negative bonuses.

        bonus.Value = GetRandomPercentageBonus(maxBonus, minBonus);
        if (bonus.Value < 0 && !bonus.BonusType.IsNegative())
        {
            // Normally positive bonuses being negative should be less common.
            if (MathsHelper.TrueWithProbability(0.8))
            {
                bonus.Value *= -1;
            }
        }

        bonus.Value = ApplyScaling(bonus.Value, petLevel, userLevel);

        // Check this won't cause negative bonuses to go far
        if (bonus.Value < 0 && existingBonuses?.Count > 0)
        {
            double currentTotal = existingBonuses.Where(p => p.BonusType == bonus.BonusType).Sum(x => x.Value);
            double newTotal = currentTotal + bonus.Value;
            if (newTotal < -1)
            {
                validBonus = false;
            }
        }

        return validBonus;
    }

    private static double GetRandomPercentageBonus(double maxValue = 1, double minValue = -1)
    {
        double random = GetRandomDouble();
        return minValue + (random * (maxValue - minValue));
    }

    private static double GetRandomDouble()
    {
        const int maxValue = 1001;
        const double maxDoubleVal = maxValue;
        return RandomNumberGenerator.GetInt32(1, maxValue) / maxDoubleVal;
    }

    private static double ScaleCorruptBonus(double value, Rarity rarity) => value * RandomNumberGenerator.GetInt32(2, 3 + (int)rarity);
}