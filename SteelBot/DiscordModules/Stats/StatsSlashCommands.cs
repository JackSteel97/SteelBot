using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.Logging;
using SteelBot.Channels.Stats;
using SteelBot.DiscordModules.Stats.Providers;
using SteelBot.Helpers.Extensions;
using SteelBot.Responders;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Stats;

[SlashCommandGroup("Stats", "Commands for viewing user stats and levels")]
[SlashRequireGuild]
public class StatsSlashCommands : InstrumentedApplicationCommandModule
{
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly ILogger<StatsSlashCommands> _logger;
    private readonly StatsCommandsChannel _channel;
    private readonly CancellationService _cancellationService;

    /// <inheritdoc />
    public StatsSlashCommands(ErrorHandlingService errorHandlingService, ILogger<StatsSlashCommands> logger, StatsCommandsChannel channel, CancellationService cancellationService)
        : base(logger)
    {
        _errorHandlingService = errorHandlingService;
        _logger = logger;
        _channel = channel;
        _cancellationService = cancellationService;
    }
    
    [SlashCommand("Theirs", "Displays the given user's statistics for this server")]
    [SlashCooldown(3, 30, SlashCooldownBucketType.User)]
    public async Task TheirStats(InteractionContext context, [Option("Target", "The user to view the stats for")] DiscordUser discordUser)
    {
        var discordMember = (DiscordMember)discordUser;
        _logger.LogInformation("User {UserId} requested to view User {TargetId} stats", context.Member.Id, discordUser.Id);
        var message = new StatsCommandAction(StatsCommandActionType.ViewPersonalStats, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild, discordMember);
        await _channel.Write(message, _cancellationService.Token);
    }

    [SlashCommand("Me", "Displays your user statistics for this server")]
    [SlashCooldown(3, 30, SlashCooldownBucketType.User)]
    public async Task MyStats(InteractionContext context)
    {
        _logger.LogInformation("[Slash Command] User {UserId} requested to view Their Stats", context.Member.Id);
        var message = new StatsCommandAction(StatsCommandActionType.ViewPersonalStats, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild);
        await _channel.Write(message, _cancellationService.Token);
    }

    [SlashCommand("Breakdown", "Displays a breakdown of the given user's XP values")]
    [SlashRequirePermissions(Permissions.Administrator)]
    [SlashCooldown(2, 30, SlashCooldownBucketType.User)]
    public async Task StatsBreakdown(InteractionContext context, [Option("Target", "The user to view the XP breakdown for")] DiscordUser discordUser)
    {
        var discordMember = (DiscordMember)discordUser;
        _logger.LogInformation("[Slash Command] User {UserId} requested to view Stats Breakdown for User {Target}", context.Member.Id, discordUser.Id);
        var message = new StatsCommandAction(StatsCommandActionType.ViewBreakdown, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild, discordMember);
        await _channel.Write(message, _cancellationService.Token);
    }

    [SlashCommand("MetricLeaderboard", "Displays the Top 50 leaderboard sorted by the given metric")]
    [SlashCooldown(2, 60, SlashCooldownBucketType.Channel)]
    public async Task MetricLeaderboard(InteractionContext context, [ChoiceProvider(typeof(MetricChoiceProvider))] [Option("Metric", "The metric to sort the leaderboard by")] string metric)
    {
        _logger.LogInformation("[Slash Command] User {UserId} requested to view {Metric} Leaderboard", context.Member.Id, metric);
        var message = new StatsCommandAction(StatsCommandActionType.ViewMetricLeaderboard, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild, metric: metric);
        await _channel.Write(message, _cancellationService.Token);
    }

    [SlashCommand("Leaderboard", "Displays a leaderboard of levels for this server")]
    [SlashCooldown(2, 60, SlashCooldownBucketType.Channel)]
    public async Task LevelsLeaderboard(InteractionContext context, [Option("Top", "How many users to display in the leaderboard")] [Maximum(1000)] [Minimum(1)] long top = 100)
    {
        _logger.LogInformation("[Slash Command] User {UserId} requested to view Levels Leaderboard for top {Top} users", context.Member.Id, top);
        var message = new StatsCommandAction(StatsCommandActionType.ViewLevelsLeaderboard, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild, top: top);
        await _channel.Write(message, _cancellationService.Token);
    }

    [SlashCommand("All", "Displays the leaderboard but with all statistics detail for each user")]
    [SlashCooldown(1, 60, SlashCooldownBucketType.Channel)]
    public async Task AllStats(InteractionContext context, [Option("Top", "How many users to display in the leaderboard")] [Maximum(1000)] [Minimum(1)] long top = 10)
    {
        _logger.LogInformation("[Slash Command] User {UserId} requested to view All Stats for top {Top} users", context.Member.Id, top);
        var message = new StatsCommandAction(StatsCommandActionType.ViewAll, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild, top: top);
        await _channel.Write(message, _cancellationService.Token);
    }

    [SlashCommand("Velocity", "Show the current XP velocity for a given user")]
    [SlashCooldown(5, 60, SlashCooldownBucketType.Channel)]
    [SlashRequirePermissions(Permissions.Administrator)]
    public async Task Velocity(InteractionContext context, [Option("Target", "The user to view the XP velocity for")] DiscordUser target)
    {
        var discordMember = (DiscordMember)target;
        _logger.LogInformation("[Slash Command] User {UserId} requested to view the XP velocity of {TargetId}", context.Member.Id, target.Id);

        var message = new StatsCommandAction(StatsCommandActionType.ViewVelocity, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild, discordMember);
        await _channel.Write(message, _cancellationService.Token);
    }

    [ContextMenu(ApplicationCommandType.UserContextMenu, "Get Stats")]
    [SlashCooldown(2, 60, SlashCooldownBucketType.User)]
    public async Task StatsContextMenu(ContextMenuContext context)
    {
        var discordMember = (DiscordMember)context.TargetUser;
        _logger.LogInformation("[Context Menu] User {UserId} requested to view User {TargetId} stats", context.Member.Id, discordMember.Id);
        var message = new StatsCommandAction(StatsCommandActionType.ViewPersonalStats, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild, discordMember);
        await _channel.Write(message, _cancellationService.Token);
    }
    
    [ContextMenu(ApplicationCommandType.UserContextMenu, "Get XP Breakdown")]
    [SlashRequirePermissions(Permissions.Administrator)]
    [SlashCooldown(2, 30, SlashCooldownBucketType.User)]
    public async Task StatsBreakdownContextMenu(ContextMenuContext context)
    {
        var discordMember = (DiscordMember)context.TargetUser;
        _logger.LogInformation("[Context Menu] User {UserId} requested to view Stats Breakdown for User {Target}", context.Member.Id, discordMember.Id);
        var message = new StatsCommandAction(StatsCommandActionType.ViewBreakdown, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild, discordMember);
        await _channel.Write(message, _cancellationService.Token);
    }
    
    [ContextMenu(ApplicationCommandType.UserContextMenu, "Get XP Velocity")]
    [SlashCooldown(5, 60, SlashCooldownBucketType.Channel)]
    [SlashRequirePermissions(Permissions.Administrator)]
    public async Task Velocity(ContextMenuContext context)
    {
        var discordMember = (DiscordMember)context.TargetUser;
        _logger.LogInformation("[Slash Command] User {UserId} requested to view the XP velocity of {TargetId}", context.Member.Id, discordMember.Id);

        var message = new StatsCommandAction(StatsCommandActionType.ViewVelocity, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild, discordMember);
        await _channel.Write(message, _cancellationService.Token);
    }
}