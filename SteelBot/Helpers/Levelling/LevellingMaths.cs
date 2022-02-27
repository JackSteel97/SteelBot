using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using System;
using System.Collections.Generic;

namespace SteelBot.Helpers.Levelling
{
    public static class LevellingMaths
    {
        public static ulong XpForLevel(int level)
        {
            // Xp = (1.2^level)-1 + (500*level)
            return Convert.ToUInt64(Math.Round((Math.Pow(1.2, level) - 1) + (500 * level)));
        }

        public static bool UpdateLevel(int currentLevel, double totalXp, out int newLevel)
        {
            newLevel = currentLevel;

            bool hasEnoughXp;
            do
            {
                ulong requiredXp = XpForLevel(newLevel + 1);
                hasEnoughXp = totalXp >= requiredXp;
                if (hasEnoughXp)
                {
                    ++newLevel;
                }
            } while (hasEnoughXp);

            return newLevel > currentLevel;
        }

        public static ulong GetDurationXp(TimeSpan duration, TimeSpan existingDuration, List<Pet> availablePets, BonusType bonusType, double baseXp = 1)
        {
            var durationXp = GetDurationXp(duration, existingDuration, baseXp);
            return ApplyPetBonuses(durationXp, availablePets, bonusType);
        }

        public static ulong GetDurationXp(TimeSpan duration, TimeSpan existingDuration, double baseXp = 1)
        {
            TimeSpan AWeek = TimeSpan.FromDays(7);

            double multiplier = 1 + (existingDuration / AWeek);

            double totalXp = duration.TotalMinutes * baseXp * multiplier;

            return Convert.ToUInt64(Math.Round(totalXp, MidpointRounding.AwayFromZero));
        }

        public static ulong ApplyPetBonuses(ulong baseXp, List<Pet> availablePets, BonusType requiredBonus)
        {
            double multiplier = 1;
            foreach (var pet in availablePets)
            {
                foreach (var bonus in pet.Bonuses)
                {
                    if (bonus.BonusType.HasFlag(requiredBonus))
                    {
                        if (bonus.BonusType.IsNegative())
                        {
                            multiplier -= bonus.PercentageValue;
                        }
                        else
                        {
                            multiplier += bonus.PercentageValue;
                        }
                    }
                }
            }

            var multipliedXp = Math.Round(baseXp * multiplier);
            var earnedXp = Convert.ToUInt64(Math.Max(0, multipliedXp)); // Prevent negative from massive negative bonuses.
            IncrementPetXp(earnedXp, availablePets);
            return earnedXp;
        }

        public static void IncrementPetXp(ulong userEarnedXp, List<Pet> pets)
        {
            const double percentageForPrimary = 0.5;
            const double percentageForOthers = 0.01;

            foreach (var pet in pets)
            {
                double earnedXp;
                if (pet.IsPrimary)
                {
                    earnedXp = userEarnedXp * percentageForPrimary;
                }
                else
                {
                    earnedXp = userEarnedXp * percentageForOthers;
                }
                pet.EarnedXp += earnedXp;
            }
        }
    }
}