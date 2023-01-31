using Microsoft.Extensions.Logging;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.Helpers.Levelling;
using System;
using System.Security.Cryptography;

namespace SteelBot.DiscordModules.Pets.Helpers;

public static class PetMaths
{
    public static double CalculateTreatXp(int petLevel, Rarity petRarity, double petTreatXpBonus, bool isCorrupt, ILogger logger)
    {
        const string formatter = "N0";
        const int lowerBound = 100;
        double xpRequiredForNextLevel = LevellingMaths.PetXpForLevel(petLevel + 1, petRarity, isCorrupt);
        double xpRequiredForThisLevel = LevellingMaths.PetXpForLevel(petLevel, petRarity, isCorrupt);
        double xpRequiredToLevel = xpRequiredForNextLevel - xpRequiredForThisLevel;
        logger.LogDebug("XP currently required to reach Level {NextLevel} from {CurrentLevel} is {XpRequiredToLevel}", petLevel + 1, petLevel, xpRequiredToLevel.ToString(formatter));

        double scaledXp = xpRequiredToLevel / (1 + Math.Log(Math.Max(1, petLevel - 50), 1.5));
        logger.LogDebug("After applying the scaling algorithm the max XP we can grant is {ScaledXp}", scaledXp.ToString(formatter));

        double upperBound = Math.Max(lowerBound + 1, Math.Round(scaledXp));
        double randomMultiplier = RandomNumberGenerator.GetInt32(101) / 100d;
        double xpGain = (randomMultiplier * (upperBound - lowerBound)) + lowerBound;
        logger.LogDebug("The Upper Bound is {UpperBound}, the Lower Bound is {LowerBound}, the Random Co-efficient is {RandomMultiplier}", upperBound.ToString(formatter), lowerBound.ToString(formatter), randomMultiplier);

        double finalXp = Math.Round(xpGain * petTreatXpBonus);
        logger.LogDebug("The Xp Gain calculated is {XpGain}, The Pet Treat Bonus is {PetTreatBonus}", xpGain.ToString(formatter), petTreatXpBonus);
        logger.LogDebug("The final calculated treat Xp is {FinalXp}", finalXp.ToString(formatter));
        return finalXp;
    }
}