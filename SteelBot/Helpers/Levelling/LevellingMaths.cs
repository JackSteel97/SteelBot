using System;

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
    }
}