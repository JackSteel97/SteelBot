using Microsoft.Extensions.Logging;
using SteelBot.DiscordModules.Stats.Services;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.Channels.Stats;

public class StatsCommandsChannel : BaseChannel<StatsCommandAction>
{
    private readonly StatsAdminService _statsAdminService;
    private readonly StatsCardService _statsCardService;
    private readonly StatsLeaderboardService _statsLeaderboardService;

    /// <inheritdoc />
    public StatsCommandsChannel(StatsLeaderboardService statsLeaderboardService,
        StatsCardService statsCardService,
        StatsAdminService statsAdminService,
        ILogger<StatsCommandsChannel> logger,
        ErrorHandlingService errorHandlingService,
        string channelLabel = "Stats")
        : base(logger, errorHandlingService, channelLabel)
    {
        _statsLeaderboardService = statsLeaderboardService;
        _statsCardService = statsCardService;
        _statsAdminService = statsAdminService;
    }

    /// <inheritdoc />
    protected override ValueTask HandleMessage(StatsCommandAction message)
    {
        Task.Run(async () =>
        {
            switch (message.Action)
            {
                case StatsCommandActionType.ViewMetricLeaderboard:
                    await _statsLeaderboardService.MetricLeaderboard(message);
                    break;
                case StatsCommandActionType.ViewLevelsLeaderboard:
                    await _statsLeaderboardService.LevelsLeaderboard(message);
                    break;
                case StatsCommandActionType.ViewAll:
                    await _statsLeaderboardService.AllStats(message);
                    break;
                case StatsCommandActionType.ViewPersonalStats:
                    await _statsCardService.View(message);
                    break;
                case StatsCommandActionType.ViewBreakdown:
                    await _statsAdminService.Breakdown(message);
                    break;
                case StatsCommandActionType.ViewVelocity:
                    await _statsAdminService.Velocity(message);
                    break;
            }
        }).FireAndForget(ErrorHandlingService);

        return ValueTask.CompletedTask;
    }
}