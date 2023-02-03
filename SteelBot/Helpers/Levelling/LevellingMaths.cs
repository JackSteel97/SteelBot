using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Helpers;
using System;
using System.Collections.Generic;

namespace SteelBot.Helpers.Levelling;

public static class LevellingMaths
{
    public static double XpForLevel(int level)
    {
        // XP = High-Level Scale + Low-Level Scale + Base Scale
        // High-Level Scale = (1.2^level)-1
        // Low-Level Scale = (level^2.5)*level
        // Base Scale = 500*level;
        double highLevelScale = Math.Pow(1.2, level) - 1;
        double lowLevelScale = Math.Pow(level, 2.5) * level;
        double baseScale = 500 * level;
        return Math.Round(highLevelScale + lowLevelScale + baseScale);
    }

    public static double PetXpForLevel(int level, Rarity rarity, bool isCorrupt)
    {
        // Pets start at level 1.
        if (level == 1) return 0;

        double multiplier = 1 + (((rarity - Rarity.Rare) / 10D)*2);
        if (isCorrupt)
        {
            multiplier += 1;
        }

        return XpForLevel(level) * multiplier;
    }

    public static bool UpdateLevel(int currentLevel, double totalXp, out int newLevel)
    {
        newLevel = currentLevel;

        bool hasEnoughXp;
        do
        {
            double requiredXp = XpForLevel(newLevel + 1);
            hasEnoughXp = totalXp >= requiredXp;
            if (hasEnoughXp) ++newLevel;
        } while (hasEnoughXp);

        return newLevel > currentLevel;
    }

    public static bool UpdatePetLevel(int currentLevel, double totalXp, Rarity rarity, bool isCorrupt, out int newLevel)
    {
        newLevel = currentLevel;
        bool hasEnoughXp;
        do
        {
            double requiredXp = PetXpForLevel(newLevel + 1, rarity, isCorrupt);
            hasEnoughXp = totalXp >= requiredXp;
            if (hasEnoughXp) ++newLevel;
        } while (hasEnoughXp);

        return newLevel > currentLevel;
    }

    public static ulong GetDurationXp(TimeSpan duration, TimeSpan existingDuration, List<Pet> availablePets, BonusType bonusType, double baseXp = 1)
    {
        ulong durationXp = GetDurationXp(duration, existingDuration, baseXp);
        return ApplyPetBonuses(durationXp, availablePets, bonusType);
    }

    public static ulong GetDurationXp(TimeSpan duration, TimeSpan existingDuration, double baseXp = 1)
    {
        var aWeek = TimeSpan.FromDays(7);

        double multiplier = 1 + (existingDuration / aWeek);

        double totalXp = duration.TotalMinutes * baseXp * multiplier;

        return Convert.ToUInt64(Math.Round(totalXp, MidpointRounding.AwayFromZero));
    }

    public static ulong ApplyPetBonuses(ulong baseXp, List<Pet> activePets, BonusType requiredBonus)
    {
        double multiplier = 1;
        foreach (var pet in activePets)
        foreach (var bonus in pet.Bonuses)
            if (bonus.BonusType.HasFlag(requiredBonus))
                multiplier += bonus.Value;

        double multipliedXp = Math.Round(baseXp * multiplier);
        ulong earnedXp = Convert.ToUInt64(Math.Max(0, multipliedXp)); // Prevent negative from massive negative bonuses.
        if (earnedXp > 0) IncrementPetXp(earnedXp, activePets);
        return earnedXp;
    }

    public static ulong ApplyScalingFactor(ulong xp, double scalingFactor) => Convert.ToUInt64(Math.Ceiling(xp * scalingFactor));

    public static void IncrementPetXp(ulong userEarnedXp, List<Pet> activePets)
    {
        const double maximumPercentage = 1;
        const double minimumPercentage = 0.01;
        double sharedXpMultiplier = PetShared.GetBonusValue(activePets, BonusType.PetSharedXp);

        foreach (var pet in activePets)
        {
            int priorityDivisor = (pet.Priority + 1) * 2;
            double thisPercentage = Math.Max(maximumPercentage / priorityDivisor * sharedXpMultiplier, minimumPercentage);
            double earnedXp = userEarnedXp * thisPercentage;

            pet.EarnedXp += earnedXp;
        }
    }
}