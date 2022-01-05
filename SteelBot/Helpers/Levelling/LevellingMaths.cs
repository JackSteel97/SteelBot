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

        public static ulong GetDurationXp(TimeSpan duration, double baseXp = 1)
        {
            TimeSpan AWeek = TimeSpan.FromDays(7);

            int weeks = (int)Math.Ceiling(duration.TotalDays / 7);

            double totalXp = 0;
            TimeSpan remainingTime = duration;
            for (int week = 1; week <= weeks; week++)
            {
                if (remainingTime.TotalMinutes < AWeek.TotalMinutes)
                {
                    totalXp += remainingTime.TotalMinutes * baseXp * week;
                }
                else
                {
                    totalXp += AWeek.TotalMinutes * baseXp * week;
                    remainingTime = remainingTime.Subtract(AWeek);
                }
            }
            return Convert.ToUInt64(Math.Round(totalXp));
        }
    }
}