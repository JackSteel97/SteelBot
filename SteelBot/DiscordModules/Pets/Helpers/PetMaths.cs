using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.Helpers.Levelling;
using System;
using System.Security.Cryptography;

namespace SteelBot.DiscordModules.Pets.Helpers;

public static class PetMaths
{
    public static double CalculateTreatXp(int petLevel, Rarity petRarity, double petTreatXpBonus)
    {
        const int lowerBound = 100;
        double xpRequiredForNextLevel = LevellingMaths.PetXpForLevel(petLevel + 1, petRarity);
        double xpRequiredForThisLevel = LevellingMaths.PetXpForLevel(petLevel, petRarity);
        double xpRequiredToLevel = xpRequiredForNextLevel - xpRequiredForThisLevel;
        double scaledXp = xpRequiredToLevel / (1 + Math.Log(Math.Max(1, petLevel-50), 1.5));
        double upperBound = Math.Max(lowerBound+1, Math.Round(scaledXp));
        double randomMultiplier = RandomNumberGenerator.GetInt32(101) / 100d;
        double xpGain = (randomMultiplier * (upperBound-lowerBound)) + lowerBound;
        return Math.Round(xpGain * petTreatXpBonus);
    }
}