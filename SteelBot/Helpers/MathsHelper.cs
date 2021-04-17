using System;

namespace SteelBot.Helpers
{
    public static class MathsHelper
    {
        public static decimal PercentageChange(decimal oldValue, decimal newValue)
        {
            if (oldValue != 0)
            {
                return (newValue - oldValue) / Math.Abs(oldValue);
            }
            return newValue / 100;
        }

        /// <summary>
        /// Get a multiplier based on the amount of weeks in a duration.
        /// 1 based.
        /// </summary>
        /// <param name="duration">Duration to use.</param>
        /// <returns>A floored multiplier</returns>
        public static double GetMultiplier(TimeSpan duration)
        {
            double weeks = Math.Floor(duration.TotalDays / 7);
            return 1 + weeks;
        }

        public static double GetPercentageOfDuration(TimeSpan smallerDuration, TimeSpan largerDuration)
        {
            return smallerDuration.TotalSeconds / largerDuration.TotalSeconds;
        }
    }
}