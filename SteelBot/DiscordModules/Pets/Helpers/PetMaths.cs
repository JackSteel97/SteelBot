using Microsoft.Extensions.Logging;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.Helpers.Levelling;
using System;
using System.Security.Cryptography;

namespace SteelBot.DiscordModules.Pets.Helpers;

public static class PetMaths
{
    public static double CalculateTreatXp(int petLevel, Rarity petRarity, double petTreatXpBonus, int userLevel, ILogger logger)
    {
        const string formatter = "N0";
        const int lowerBound = 100;

        int nextUserLevel = userLevel + 1;
        int differenceBetweenLevels = Math.Abs(userLevel - petLevel);

        double xpRequiredForNextUserLevel = LevellingMaths.XpForLevel(nextUserLevel);
        double xpRequiredForThisUserLevel = LevellingMaths.XpForLevel(userLevel);
        double xpRequiredToLevelTheUser = xpRequiredForNextUserLevel - xpRequiredForThisUserLevel;
        logger.LogDebug("XP currently required for user to reach Level {NextLevel} from {CurrentLevel} is {XpRequiredToLevel}", nextUserLevel, userLevel, xpRequiredToLevelTheUser.ToString(formatter));

        double rarityScaler = 1 + ((int)petRarity / 10D);
        logger.LogDebug("The rarity scaler is {RarityScaler}", rarityScaler);
        
        double scalingBase = (differenceBetweenLevels + 1) * rarityScaler;
        logger.LogDebug("The scaling base is {ScalingBase}", scalingBase);
        
        double scalingDivider = Math.Pow(scalingBase, 0.8) / 1.5;
        logger.LogDebug("The scaling divider is {ScalingDivider}", scalingDivider);
        
        double scaledXp = xpRequiredToLevelTheUser / scalingDivider;
        logger.LogDebug("After applying the scaling algorithm the max XP we can grant is {ScaledXp}", scaledXp.ToString(formatter));

        double upperBound = Math.Max(lowerBound + 1, Math.Round(scaledXp));
        double randomMultiplier = RandomNumberGenerator.GetInt32(101) / 100d;
        double xpGain = (randomMultiplier * (upperBound - lowerBound)) + lowerBound;
        logger.LogDebug("The Upper Bound is {UpperBound}, the Lower Bound is {LowerBound}, the Random Co-efficient is {RandomMultiplier}", upperBound.ToString(formatter),
            lowerBound.ToString(formatter), randomMultiplier);

        double finalXp = Math.Round(xpGain * petTreatXpBonus);
        logger.LogDebug("The Xp Gain calculated is {XpGain}, The Pet Treat Bonus is {PetTreatBonus}", xpGain.ToString(formatter), petTreatXpBonus);
        logger.LogDebug("The final calculated treat Xp is {FinalXp}", finalXp.ToString(formatter));
        return finalXp;
    }
}