using System;

namespace SteelBot.Helpers.Extensions;

public static class NumberExtensions
{
    private const double _thousand = 1_000;
    private const double _million = 1_000_000;
    private const double _billion = 1_000_000_000;

    public static string KiloFormat(this ulong value)
    {
        if (value >= 100 * _billion)
            return (value / _billion).ToString("0.#") + "B";
        if (value >= 10 * _billion)
            return (value / _billion).ToString("0.##") + "B";
        if (value >= _billion)
            return (value / _billion).ToString("0.###") + "B";
        if (value >= 100 * _million)
            return (value / _million).ToString("0.#") + "M";
        if (value >= 10 * _million)
            return (value / _million).ToString("0.##") + "M";
        if (value >= _million)
            return (value / _million).ToString("0.##") + "M";
        if (value >= 100 * _thousand)
            return (value / _thousand).ToString("N0") + "K";
        return value >= 10 * _thousand ? (value / _thousand).ToString("0.##") + "K" : value.ToString("N0");
    }

    public static string KiloFormat(this long value)
    {
        ulong magnitude = (ulong)Math.Abs(value);
        string formatted = KiloFormat(magnitude);
        return value < 0 ? $"-{formatted}" : formatted;
    }

    public static string ToUserMention(this ulong id) => $"<@{id}>";

    public static string ToRoleMention(this ulong id) => $"<@&{id}>";
}