using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace SteelBot.DiscordModules.Pets.Generation;
public static class PetBonusFactory
{
    private static readonly BonusType[] _excludedTypes = new BonusType[] { BonusType.None };

    public static List<PetBonus> GenerateMany(Pet pet, int levelOfUser)
    {
        var maxBonuses = pet.Rarity.GetStartingBonusCount();
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
            bonus.BonusType = GetWeightedRandomBonusType(pet.Rarity);

            if (bonus.BonusType.IsPercentage())
            {
                validBonus = HandlePercentageBonusGeneration(pet, maxPercentageBonus, bonus, existingBonuses);
            }
            else if (bonus.BonusType == BonusType.OfflineXP)
            {
                HandleOfflineXpBonusGeneration(bonus, pet.Rarity, pet.CurrentLevel, levelOfUser);
            }
            else
            {
                HandleIntegerBonusGeneration(bonus, pet.Rarity);
            }
        } while (!validBonus);

        return bonus;
    }

    private static BonusType GetWeightedRandomBonusType(Rarity rarity)
    {
        double rarityValue = (double)rarity;
        double chanceToGeneratePassiveXp = rarityValue / 10;
        double chanceToGeneratePetSlots = rarityValue / 20;

        if (MathsHelper.TrueWithProbability(chanceToGeneratePassiveXp))
        {
            return BonusType.OfflineXP;
        }

        if (MathsHelper.TrueWithProbability(chanceToGeneratePetSlots))
        {
            return BonusType.PetSlots;
        }

        return PetGenerationShared.GetRandomEnumValue<BonusType>(_excludedTypes);
    }

    private static void HandleOfflineXpBonusGeneration(PetBonus bonus, Rarity rarity, int petLevel, int userLevel)
    {
        double baseValue = (double)rarity;

        double chanceToGetMore = baseValue / 10;

        double petLevelMultiplier = 1 + ((double)petLevel / 100);
        double userLevelMultiplier = 1 + ((double)userLevel / 100);
        baseValue *= petLevelMultiplier;
        baseValue *= userLevelMultiplier;
        bonus.Value = baseValue;

        while (MathsHelper.TrueWithProbability(chanceToGetMore))
        {
            bonus.Value += baseValue;
        }
    }

    private static void HandleIntegerBonusGeneration(PetBonus bonus, Rarity rarity)
    {
        bonus.Value = 1;
        while (MathsHelper.TrueWithProbability(0.1))
        {
            ++bonus.Value;
        }

        var probabilityToGoNegative = ((double)rarity + 1) / 10;
        if (MathsHelper.TrueWithProbability(probabilityToGoNegative))
        {
            bonus.Value *= -1;
        }
    }

    private static bool HandlePercentageBonusGeneration(Pet pet, double maxBonus, PetBonus bonus, List<PetBonus> existingBonuses)
    {
        bool validBonus = true;
        var minBonus = pet.Rarity < Rarity.Rare && !bonus.BonusType.IsNegative() ? 0 : maxBonus * -1; // Lower rarities shouldn't have negative bonuses.

        bonus.Value = GetRandomPercentageBonus(maxBonus, minBonus);
        if (bonus.Value < 0 && !bonus.BonusType.IsNegative())
        {
            // Normally positive bonuses being negative should be less common.
            if (MathsHelper.TrueWithProbability(0.8))
            {
                bonus.Value *= -1;
            }
        }

        // Check this won't cause negative bonuses to go far
        if (bonus.Value < 0 && existingBonuses?.Count > 0)
        {
            var currentTotal = existingBonuses.Where(p => p.BonusType == bonus.BonusType).Sum(x => x.Value);
            var newTotal = currentTotal + bonus.Value;
            if (newTotal < -1)
            {
                validBonus = false;
            }
        }

        return validBonus;
    }

    private static double GetRandomPercentageBonus(double maxValue = 1, double minValue = -1)
    {
        var random = GetRandomDouble();
        return minValue + (random * (maxValue - minValue));
    }

    private static double GetRandomDouble()
    {
        const int maxValue = 1001;
        const double maxDoubleVal = maxValue;
        return RandomNumberGenerator.GetInt32(1, maxValue) / maxDoubleVal;
    }
}
