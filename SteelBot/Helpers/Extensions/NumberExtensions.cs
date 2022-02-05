using System;

namespace SteelBot.Helpers.Extensions
{
    public static class NumberExtensions
    {
        private const double Thousand = 1_000;
        private const double Million = 1_000_000;
        private const double Billion = 1_000_000_000;

        public static string KiloFormat(this ulong value)
        {
            if (value >= 100 * Billion)
                return (value / Billion).ToString("0.#") + "B";
            if (value >= 10 * Billion)
                return (value / Billion).ToString("0.##") + "B";
            if (value >= Billion)
                return (value / Billion).ToString("0.###") + "B";
            if (value >= 100 * Million)
                return (value / Million).ToString("0.#") + "M";
            if (value >= 10 * Million)
                return (value / Million).ToString("0.##") + "M";
            if (value >= Million)
                return (value / Million).ToString("0.##") + "M";
            if (value >= 100 * Thousand)
                return (value / Thousand).ToString("N0") + "K";
            if (value >= 10 * Thousand)
                return (value / Thousand).ToString("0.##") + "K";

            return value.ToString("N0");
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

        public static string ToMention(this ulong id)
        {
            return $"<@{id}>";
        }
    }
}