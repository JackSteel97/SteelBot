using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.DiscordModules.AuditLog.Services;
using SteelBot.Helpers.Extensions;
using SteelBot.Responders;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Utility;

[Group("Utility")]
[Description("Helpful functions.")]
[Aliases("util", "u")]
public class UtilityCommands : TypingCommandModule
{
    private readonly UtilityService _utilityService;
    private readonly ErrorHandlingService _errorHandlingService;

    /// <inheritdoc />
    public UtilityCommands(UtilityService utilityService, ErrorHandlingService errorHandlingService, ILogger<UtilityCommands> logger, IHub sentry, AuditLogService auditLogService) : base(logger, auditLogService, sentry)
    {
        _utilityService = utilityService;
        _errorHandlingService = errorHandlingService;   
    }

    [Command("ChannelsInfo")]
    [Aliases("sci")]
    [Description("Displays various information about this server's channels.")]
    [Cooldown(2, 300, CooldownBucketType.Guild)]
    public Task ChannelsInfo(CommandContext context)
    {
        _utilityService.ChannelsInfo(context.Guild, new MessageResponder(context, _errorHandlingService));
        return Task.CompletedTask;
    }

    [Command("ServerInfo")]
    [Aliases("si")]
    [Description("Displays various information about this server.")]
    [Cooldown(2, 300, CooldownBucketType.Guild)]
    public Task ServerInfo(CommandContext context)
    {
       _utilityService.ServerInfo(context.Guild, new MessageResponder(context, _errorHandlingService));
       return Task.CompletedTask;
    }

    [Command("Status")]
    [Aliases("s", "uptime", "info")]
    [Description("Displays various information about the current status of the bot.")]
    [Cooldown(2, 300, CooldownBucketType.Channel)]
    public Task BotStatus(CommandContext context)
    {
        _utilityService.BotStatus(context.Message.CreationTimestamp, context.Client, new MessageResponder(context, _errorHandlingService));
        return Task.CompletedTask;
    }

    [Command("Ping")]
    [Description("Pings the bot.")]
    [Cooldown(10, 60, CooldownBucketType.Channel)]
    public Task Ping(CommandContext context)
    {
        _utilityService.Ping(new MessageResponder(context, _errorHandlingService));
        return Task.CompletedTask;
    }

    [Command("Choose")]
    [Aliases("PickFrom", "Pick", "Select", "pf")]
    [Description("Select x options randomly from a given list.")]
    [Cooldown(5, 60, CooldownBucketType.Channel)]
    public Task Choose(CommandContext context, int numberToSelect, params string[] options)
    {
        _utilityService.Choose(new MessageResponder(context, _errorHandlingService), numberToSelect, options);
        return Task.CompletedTask;
    }

    [Command("FlipCoin")]
    [Aliases("TossCoin", "fc", "flip")]
    [Description("Flips a coin.")]
    [Cooldown(10, 60, CooldownBucketType.User)]
    public Task FlipCoin(CommandContext context)
    {
        _utilityService.FlipCoin(new MessageResponder(context, _errorHandlingService));
        return Task.CompletedTask;
    }

    [Command("RollDie")]
    [Aliases("Roll", "rd")]
    [Description("Rolls a die.")]
    [Cooldown(10, 60, CooldownBucketType.User)]
    public Task RollDie(CommandContext context, int sides = 6)
    {
        _utilityService.RollDie(new MessageResponder(context, _errorHandlingService), sides);
        return Task.CompletedTask;
    }

    [Command("Speak")]
    [Description("Get the bot to post the given message in a channel.")]
    [RequireUserPermissions(Permissions.Administrator)]
    [Cooldown(1, 60, CooldownBucketType.Guild)]
    public Task Speak(CommandContext context, DiscordChannel channel, string title, string content, string footerContent = "")
    {
        return _utilityService.Speak(new MessageResponder(context, _errorHandlingService), context.Guild, channel, title, content, footerContent);
    }

    [Command("shutdown")]
    [Description("Gracefully shuts down the bot")]
    [RequireOwner]
    public Task Shutdown(CommandContext context)
    {
        return _utilityService.Shutdown(new MessageResponder(context, _errorHandlingService), context.Member);
    }

    [Command("logs")]
    [Description("Send the current log file.")]
    [RequireOwner]
    public Task GetLogs(CommandContext context)
    {
        return _utilityService.GetLogs(new MessageResponder(context, _errorHandlingService));
    }
}