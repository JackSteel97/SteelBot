using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.Channels.Stats;
using SteelBot.DiscordModules.AuditLog.Services;
using SteelBot.Helpers.Extensions;
using SteelBot.Responders;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Stats;

[Group("Stats")]
[Description("Commands for viewing user stats and levels")]
[RequireGuild]
public class StatsCommands : TypingCommandModule
{
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly ILogger<StatsCommands> _logger;
    private readonly StatsCommandsChannel _statsCommandsChannel;
    private readonly CancellationService _cancellationService;
    
    public StatsCommands(ErrorHandlingService errorHandlingService, IHub sentry, ILogger<StatsCommands> logger, StatsCommandsChannel statsCommandsChannel, CancellationService cancellationService, AuditLogService auditLogService)
        : base(logger, auditLogService, sentry)
    {
        _errorHandlingService = errorHandlingService;
        _logger = logger;
        _statsCommandsChannel = statsCommandsChannel;
        _cancellationService = cancellationService;
    }

    [GroupCommand]
    [Description("Displays the given user's statistics for this server.")]
    [Cooldown(1, 30, CooldownBucketType.User)]
    public async Task TheirStats(CommandContext context, DiscordMember discordUser)
    {
        _logger.LogInformation("User {UserId} requested to view User {TargetId} stats", context.Member.Id, discordUser.Id);
        var message = new StatsCommandAction(StatsCommandActionType.ViewPersonalStats, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild, discordUser);
        await _statsCommandsChannel.Write(message, _cancellationService.Token);
    }

    [Command("me")]
    [Aliases("mine")]
    [Description("Displays your user statistics for this server.")]
    [Cooldown(3, 30, CooldownBucketType.User)]
    public async Task MyStats(CommandContext context)
    {
        _logger.LogInformation("User {UserId} requested to view Their Stats", context.Member.Id);
        var message = new StatsCommandAction(StatsCommandActionType.ViewPersonalStats, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild);
        await _statsCommandsChannel.Write(message, _cancellationService.Token);
    }

    [Command("breakdown")]
    [Description("Displays a breakdown of the given user (or your own) XP values.")]
    [RequireUserPermissions(Permissions.Administrator)]
    [Cooldown(2, 30, CooldownBucketType.User)]
    public async Task StatsBreakdown(CommandContext context, DiscordMember discordUser = null)
    {
        _logger.LogInformation("User {UserId} requested to view Stats Breakdown for User {Target}", context.Member.Id, (discordUser ?? context.Member).Id);
        var message = new StatsCommandAction(StatsCommandActionType.ViewBreakdown, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild, discordUser);
        await _statsCommandsChannel.Write(message, _cancellationService.Token);
    }

    [GroupCommand]
    [Description("Displays the Top 50 leaderboard sorted by the given metric.")]
    [Cooldown(2, 60, CooldownBucketType.Channel)]
    public async Task MetricLeaderboard(CommandContext context, [RemainingText] string metric)
    {
        _logger.LogInformation("User {UserId} requested to view {Metric} Leaderboard", context.Member.Id, metric);
        var message = new StatsCommandAction(StatsCommandActionType.ViewMetricLeaderboard, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild, metric: metric);
        await _statsCommandsChannel.Write(message, _cancellationService.Token);
    }

    [Command("Leaderboard")]
    [Description("Displays a leaderboard of levels for this server.")]
    [Cooldown(2, 60, CooldownBucketType.Channel)]
    public async Task LevelsLeaderboard(CommandContext context, int top = 100)
    {
        _logger.LogInformation("User {UserId} requested to view Levels Leaderboard for top {Top} users", context.Member.Id, top);
        var message = new StatsCommandAction(StatsCommandActionType.ViewLevelsLeaderboard, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild, top: top);
        await _statsCommandsChannel.Write(message, _cancellationService.Token);
    }

    [Command("All")]
    [Description("Displays the leaderboard but with all stats detail for each user.")]
    [Cooldown(1, 60, CooldownBucketType.Channel)]
    public async Task AllStats(CommandContext context, int top = 10)
    {
        _logger.LogInformation("User {UserId} requested to view All Stats for top {Top} users", context.Member.Id, top);
        var message = new StatsCommandAction(StatsCommandActionType.ViewAll, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild, top: top);
        await _statsCommandsChannel.Write(message, _cancellationService.Token);
    }

    [Command("Velocity")]
    [Aliases("Gains")]
    [Description("Show the current XP velocity for yourself or a given user")]
    [Cooldown(5, 60, CooldownBucketType.Channel)]
    [RequirePermissions(Permissions.Administrator)]
    public async Task Velocity(CommandContext context, DiscordMember target = null)
    {
        target ??= context.Member;
        _logger.LogInformation("User {UserId} requested to view the XP velocity of {TargetId}", context.Member.Id, target.Id);

        var message = new StatsCommandAction(StatsCommandActionType.ViewVelocity, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild, target);
        await _statsCommandsChannel.Write(message, _cancellationService.Token);
    }
}