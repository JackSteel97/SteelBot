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

        public static ulong GetDurationXp(TimeSpan duration, TimeSpan existingDuration, double baseXp = 1)
        {
            TimeSpan AWeek = TimeSpan.FromDays(7);

            double multiplier = 1 + (existingDuration / AWeek);

            double totalXp = duration.TotalMinutes * baseXp * multiplier;
            
            return Convert.ToUInt64(Math.Round(totalXp));
        }

        public static ulong ApplyPetBonuses(ulong baseXp, List<Pet> availablePets, BonusType requiredBonus)
        {
            double multiplier = 1;
            foreach(var pet in availablePets)
            {
                foreach(var bonus in pet.Bonuses)
                {
                    if (bonus.BonusType.HasFlag(requiredBonus))
                    {
                        if (bonus.BonusType.IsNegative())
                        {
                            multiplier *= (1 - bonus.PercentageValue);
                        }
                        else
                        {
                            multiplier *= (1 + bonus.PercentageValue);
                        }
                    }
                }
            }

            return Convert.ToUInt64(Math.Round(baseXp * multiplier));
        } 
    }
}