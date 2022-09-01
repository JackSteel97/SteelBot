using DSharpPlus.Entities;
using SteelBot.Responders;

namespace SteelBot.Channels.Stats;

public enum StatsCommandActionType
{
    ViewPersonalStats,
    ViewBreakdown,
    ViewMetricLeaderboard,
    ViewLevelsLeaderboard,
    ViewAll,
    ViewVelocity,
}

public record StatsCommandAction : BaseAction<StatsCommandActionType>
{
    public DiscordMember Target { get; }

    public string Metric { get; }
    
    public int Top { get; }

    public StatsCommandAction(StatsCommandActionType action, IResponder responder, DiscordMember member, DiscordGuild guild, DiscordMember target = null, string metric = null, int top = 0)
    : base(action, responder, member, guild)
    {
        Target = target ?? member;
        Metric = metric;
        Top = top;
    }
}