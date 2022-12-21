using Microsoft.Extensions.Logging;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.Helpers.Levelling;
using System;
using System.Security.Cryptography;

namespace SteelBot.DiscordModules.Pets.Helpers;

public static class PetMaths
{
    public static double CalculateTreatXp(int petLevel, Rarity petRarity, double petTreatXpBonus, ILogger logger)
    {
        const int lowerBound = 100;
        double xpRequiredForNextLevel = LevellingMaths.PetXpForLevel(petLevel + 1, petRarity);
        double xpRequiredForThisLevel = LevellingMaths.PetXpForLevel(petLevel, petRarity);
        double xpRequiredToLevel = xpRequiredForNextLevel - xpRequiredForThisLevel;
        logger.LogDebug("XP currently required to reach Level {NextLevel} from {CurrentLevel} is {XpRequiredToLevel}", petLevel + 1, petLevel, xpRequiredToLevel);

        double scaledXp = xpRequiredToLevel / (1 + Math.Log(Math.Max(1, petLevel - 50), 1.5));
        logger.LogDebug("After applying the scaling algorithm the max XP we can grant is {ScaledXp}", scaledXp);

        double upperBound = Math.Max(lowerBound + 1, Math.Round(scaledXp));
        double randomMultiplier = RandomNumberGenerator.GetInt32(101) / 100d;
        double xpGain = (randomMultiplier * (upperBound - lowerBound)) + lowerBound;
        logger.LogDebug("The Upper Bound is {UpperBound}, the Lower Bound is {LowerBound}, the Random Co-efficient is {RandomMultiplier}", upperBound, lowerBound, randomMultiplier);

        double finalXp = Math.Round(xpGain * petTreatXpBonus);
        logger.LogDebug("The Xp Gain calculated is {XpGain}, The Pet Treat Bonus is {PetTreatBonus}", xpGain, petTreatXpBonus);
        logger.LogDebug("The final calculated treat Xp is {FinalXp}", finalXp);
        return finalXp;
    }
}