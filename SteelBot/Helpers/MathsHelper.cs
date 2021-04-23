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

        public static double GetPercentageOfDuration(TimeSpan smallerDuration, TimeSpan largerDuration)
        {
            return smallerDuration.TotalSeconds / largerDuration.TotalSeconds;
        }
    }
}