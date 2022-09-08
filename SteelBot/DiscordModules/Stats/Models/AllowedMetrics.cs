using DSharpPlus;
using Humanizer;
using System.Collections.Generic;
using System.Linq;

namespace SteelBot.DiscordModules.Stats.Models;

public static class AllowedMetrics
{
    public static readonly HashSet<string> Metrics = new()
    {
        "xp",
        "level",
        "message count",
        "message length",
        "afk",
        "voice",
        "muted",
        "deafened",
        "last active",
        "activity",
        "stream",
        "video"
    };

    public static string MetricsList = string.Join(", ", Metrics.Select(m => Formatter.InlineCode(m.Transform(To.TitleCase))));
}