using System;

namespace SteelBot.Helpers.Extensions
{
    public static class NumberExtensions
    {
        public static string KiloFormat(this ulong value)
        {
            if (value >= 100000000000)
                return (value / 1000000000).ToString("##,0") + " B";
            if (value >= 10000000000)
                return (value / 1000000000D).ToString("0.####") + " B";
            if (value >= 100000000)
                return (value / 1000000).ToString("##,0") + " M";
            if (value >= 10000000)
                return (value / 1000000D).ToString("0.###") + " M";
            if (value >= 100000)
                return (value / 1000).ToString("##,0") + " K";
            if (value >= 10000)
                return (value / 1000D).ToString("0.##") + " K";
            return value.ToString("##,0");
        }

        public static string KiloFormat(this long value)
        {
            ulong magnitude = (ulong)Math.Abs(value);
            string formatted = KiloFormat(magnitude);
            if (value < 0)
            {
                return $"-{formatted}";
            }
            return formatted;
        }

        public static string AsMention(this ulong id)
        {
            return $"<@{id}>";
        }
    }
}