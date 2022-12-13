using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.Logging;
using SteelBot.Helpers.Extensions;
using SteelBot.Responders;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Utility;

[SlashRequireGuild]
public class UtilitySlashCommands : InstrumentedApplicationCommandModule
{
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly CancellationService _cancellationService;
    private readonly ILogger<UtilitySlashCommands> _logger;
    private readonly UtilityService _utilityService;

    /// <inheritdoc />
    public UtilitySlashCommands(ErrorHandlingService errorHandlingService, CancellationService cancellationService, ILogger<UtilitySlashCommands> logger, UtilityService utilityService) : base(logger)
    {
        _errorHandlingService = errorHandlingService;
        _cancellationService = cancellationService;
        _logger = logger;
        _utilityService = utilityService;   
    }

    [SlashCommand("ChannelsInfo", "Displays various information about this server's channels")]
    [SlashCooldown(2, 300, SlashCooldownBucketType.Guild)]
    public Task ChannelsInfo(InteractionContext context)
    {
        _utilityService.ChannelsInfo(context.Guild, new InteractionResponder(context, _errorHandlingService));
        return Task.CompletedTask;
    }

    [SlashCommand("ServerInfo", "Displays various information about this server")]
    [SlashCooldown(2, 300, SlashCooldownBucketType.Guild)]
    public Task ServerInfo(InteractionContext context)
    {
        _utilityService.ServerInfo(context.Guild, new InteractionResponder(context, _errorHandlingService));
        return Task.CompletedTask;
    }

    [SlashCommand("Status", "Displays various information about the current status of the bot")]
    [SlashCooldown(2, 300, SlashCooldownBucketType.Channel)]
    public Task BotStatus(InteractionContext context)
    {
        _utilityService.BotStatus(context.Interaction.CreationTimestamp, context.Client, new InteractionResponder(context, _errorHandlingService));
        return Task.CompletedTask;
    }

    [SlashCommand("Ping", "Pings the bot")]
    [SlashCooldown(10, 60, SlashCooldownBucketType.Channel)]
    public Task Ping(InteractionContext context)
    {
        _utilityService.Ping(new InteractionResponder(context, _errorHandlingService));
        return Task.CompletedTask;
    }

    [SlashCommand("FlipCoin", "Flips a coin")]
    [SlashCooldown(10, 60, SlashCooldownBucketType.User)]
    public Task FlipCoin(InteractionContext context)
    {
        _utilityService.FlipCoin(new InteractionResponder(context, _errorHandlingService));
        return Task.CompletedTask;
    }

    [SlashCommand("RollDie", "Rolls a die")]
    [SlashCooldown(10, 60, SlashCooldownBucketType.User)]
    public Task RollDie(InteractionContext context, [Option("Sides", "How many sides the die has")] long sides)
    {
        _utilityService.RollDie(new InteractionResponder(context, _errorHandlingService), (int)sides);
        return Task.CompletedTask;
    }

    [SlashCommand("Speak", "Get the bot to post the given message in a channel")]
    [SlashCooldown(2, 60, SlashCooldownBucketType.Guild)]
    [SlashRequireUserPermissions(Permissions.Administrator)]
    public async Task Speak(InteractionContext context,
        [Option("Channel", "Channel to send the message to")] DiscordChannel channel,
        [Option("Title", "Embed Title")] string title,
        [Option("Content", "Embed content")] string content,
        [Option("Footer", "Embed footer")] string footerContent = "")
    {
        await context.DeferAsync();
        await _utilityService.Speak(new InteractionResponder(context, _errorHandlingService), context.Guild, channel, title, content, footerContent);
    }

    [SlashCommand("Shutdown", "Gracefully shuts down the bot")]
    [SlashRequireOwner]
    public Task Shutdown(InteractionContext context)
    {
        return _utilityService.Shutdown(new InteractionResponder(context, _errorHandlingService), (DiscordMember)context.User);
    }

    [SlashCommand("Logs", "Send the current log file")]
    [SlashRequireOwner]
    public Task GetLogs(InteractionContext context)
    {
        return _utilityService.GetLogs(new InteractionResponder(context, _errorHandlingService));
    }
}