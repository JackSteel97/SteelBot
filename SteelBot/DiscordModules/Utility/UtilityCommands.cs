using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Humanizer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.DataProviders;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Utility;

[Group("Utility")]
[Description("Helpful functions.")]
[Aliases("util", "u")]
public class UtilityCommands : TypingCommandModule
{
    private readonly Random _rand;
    private readonly AppConfigurationService _appConfigurationService;
    private readonly DataCache _cache;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<UtilityCommands> _logger;
    
    public UtilityCommands(AppConfigurationService appConfigurationService, DataCache cache, IHostApplicationLifetime applicationLifetime, IHub sentry, ILogger<UtilityCommands> logger)
        : base(logger, sentry)
    {
        _rand = new Random();
        _appConfigurationService = appConfigurationService;
        _cache = cache;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
    }

    [Command("ServerInfo")]
    [Aliases("si")]
    [Description("Displays various information about this server.")]
    [Cooldown(2, 300, CooldownBucketType.Guild)]
    public Task ServerInfo(CommandContext context)
    {
        if (!_cache.Guilds.TryGetGuild(context.Guild.Id, out var guild))
        {
            _logger.LogWarning("Guild {GuildId} is not tracked so the request to get server info failed", context.Guild.Id);
            return context.RespondAsync(embed: EmbedGenerator.Error("Something went wrong and I couldn't get this Server's info, please try again later."));
        }

        int totalUsers = context.Guild.MemberCount;
        int totalRoles = context.Guild.Roles.Count;
        int textChannels = 0, voiceChannels = 0, categories = 0;
        foreach (var channel in context.Guild.Channels)
        {
            if (channel.Value.Type == ChannelType.Text)
            {
                ++textChannels;
            }
            else if (channel.Value.Type == ChannelType.Voice)
            {
                ++voiceChannels;
            }
            else if (channel.Value.Type == ChannelType.Category)
            {
                ++categories;
            }
        }

        string created = (context.Guild.CreationTimestamp - DateTime.UtcNow).Humanize(2, maxUnit: Humanizer.Localisation.TimeUnit.Year);
        string botAdded = (guild.BotAddedTo - DateTime.UtcNow).Humanize(2, maxUnit: Humanizer.Localisation.TimeUnit.Year);
        var levelAnnouncementChannel = guild.GetLevelAnnouncementChannel(context.Guild);

        int rankRolesCount = 0;
        int selfRolesCount = 0;
        int triggersCount = 0;

        if (_cache.RankRoles.TryGetGuildRankRoles(guild.DiscordId, out var rankRoles))
        {
            rankRolesCount = rankRoles.Count;
        }

        if (_cache.SelfRoles.TryGetGuildRoles(guild.DiscordId, out var selfRoles))
        {
            selfRolesCount = selfRoles.Count;
        }

        if (_cache.Triggers.TryGetGuildTriggers(guild.DiscordId, out var triggers))
        {
            triggersCount = triggers.Count;
        }

        var builder = new DiscordEmbedBuilder().WithColor(EmbedGenerator.InfoColour)
            .WithTitle($"{context.Guild.Name} Info")
            .AddField("Total Users", Formatter.InlineCode(totalUsers.ToString()), true)
            .AddField("Roles", Formatter.InlineCode(totalRoles.ToString()), true)
            .AddField("Text Channels", Formatter.InlineCode(textChannels.ToString()), true)
            .AddField("Voice Channels", Formatter.InlineCode(voiceChannels.ToString()), true)
            .AddField("Categories", Formatter.InlineCode(categories.ToString()), true)
            .AddField("System Channel", context.Guild.SystemChannel?.Mention ?? "`None Set`", true)
            .AddField("AFK Timeout", Formatter.InlineCode(TimeSpan.FromSeconds(context.Guild.AfkTimeout).Humanize()), true)
            .AddField("AFK Channel", context.Guild.AfkChannel?.Mention ?? "`None Set`", true)
            .AddField("Created", Formatter.InlineCode($"{created} ago ({context.Guild.CreationTimestamp:dd-MMM-yyyy HH:mm})"), true)
            .AddField("Guild Id", Formatter.InlineCode(guild.DiscordId.ToString()), true)
            .AddField("Verification Level", Formatter.InlineCode(context.Guild.VerificationLevel.ToString()), true)
            .AddField("Owner", context.Guild.Owner.Mention, true)
            .AddField("SteelBot Added", Formatter.InlineCode($"{botAdded} ago ({guild.BotAddedTo:dd-MMM-yyyy HH:mm})"), true)
            .AddField("Announcement Channel", levelAnnouncementChannel?.Mention ?? "`None Set`", true)
            .AddField("Rank Roles", Formatter.InlineCode(rankRolesCount.ToString()), true)
            .AddField("Self Roles", Formatter.InlineCode(selfRolesCount.ToString()), true)
            .AddField("Triggers", Formatter.InlineCode(triggersCount.ToString()), true)
            .AddField("Good Bot Votes", Formatter.InlineCode(guild.GoodBotVotes.ToString()), true)
            .AddField("Bad Bot Votes", Formatter.InlineCode(guild.BadBotVotes.ToString()), true)
            .WithThumbnail(context.Guild.IconUrl);

        return context.RespondAsync(embed: builder.Build());
    }

    [Command("Status")]
    [Aliases("s", "uptime", "info")]
    [Description("Displays various information about the current status of the bot.")]
    [Cooldown(2, 300, CooldownBucketType.Channel)]
    public Task BotStatus(CommandContext context)
    {
        var now = DateTime.UtcNow;
        var uptime = now - _appConfigurationService.StartUpTime;
        var ping = now - context.Message.CreationTimestamp;

        var builder = new DiscordEmbedBuilder().WithColor(EmbedGenerator.InfoColour)
            .WithTitle("Bot Status")
            .AddField("Uptime", Formatter.InlineCode(uptime.Humanize(3)))
            .AddField("Processed Commands", Formatter.InlineCode(_appConfigurationService.HandledCommands.ToString()))
            .AddField("You -> Discord -> Bot Ping", Formatter.InlineCode(ping.Humanize()))
            .AddField("Bot -> Discord Ping", Formatter.InlineCode(TimeSpan.FromMilliseconds(context.Client.Ping).Humanize()))
            .AddField("Version", Formatter.InlineCode(_appConfigurationService.Version));

        return context.RespondAsync(embed: builder.Build());
    }

    [Command("Ping")]
    [Description("Pings the bot.")]
    [Cooldown(10, 60, CooldownBucketType.Channel)]
    public static Task Ping(CommandContext context)
    {
        string ret = DateTime.UtcNow.Millisecond % 5 == 0 ? "POG!" : "PONG!";
        return context.RespondAsync(embed: EmbedGenerator.Primary("", ret));
    }

    [Command("Choose")]
    [Aliases("PickFrom", "Pick", "Select", "pf")]
    [Description("Select x options randomly from a given list.")]
    [Cooldown(5, 60, CooldownBucketType.Channel)]
    public Task Choose(CommandContext context, int numberToSelect, params string[] options)
    {
        // Validation.
        if (numberToSelect <= 0)
        {
            _logger.LogWarning("Invalid Choose command request, the number to select {NumberToSelect} is less than zero", numberToSelect);
            return context.RespondAsync(embed: EmbedGenerator.Error("X must be greater than zero."));
        }

        if (options.Length == 0)
        {
            _logger.LogWarning("Invalid Choose command request, no options were provider");
            return context.RespondAsync(embed: EmbedGenerator.Error("No options were provided."));
        }

        if (numberToSelect > options.Length)
        {
            _logger.LogWarning("Invalid Choose command request, options provided {OptionsAmount} are less than the amount to select {NumberToSelect}", options.Length, numberToSelect);
            return context.RespondAsync(embed: EmbedGenerator.Error($"There are not enough options to choose {numberToSelect} unique options.\nPlease provide more options or choose less."));
        }

        var remainingOptions = options.ToList();
        var selectedOptions = new List<string>(numberToSelect);
        for (int i = 0; i < numberToSelect; i++)
        {
            // Pick random option.
            int randIndex = _rand.Next(remainingOptions.Count);
            selectedOptions.Add(remainingOptions[randIndex]);
            // Remove from possible options.
            remainingOptions.RemoveAt(randIndex);
        }

        var message = new DiscordMessageBuilder()
            .WithEmbed(EmbedGenerator.Primary(string.Join(", ", selectedOptions), $"Chosen Option{(numberToSelect > 1 ? "s" : "")}"))
            .WithReply(context.Message.Id, true);
        return context.RespondAsync(message);
    }

    [Command("FlipCoin")]
    [Aliases("TossCoin", "fc", "flip")]
    [Description("Flips a coin.")]
    [Cooldown(10, 60, CooldownBucketType.User)]
    public Task FlipCoin(CommandContext context)
    {
        int side = _rand.Next(100);
        string result = "Heads!";
        if (side < 50)
        {
            result = "Tails!";
        }

        var message = new DiscordMessageBuilder()
            .WithEmbed(EmbedGenerator.Primary(result))
            .WithReply(context.Message.Id, true);
        return context.RespondAsync(message);
    }

    [Command("RollDie")]
    [Aliases("Roll", "rd")]
    [Description("Rolls a die.")]
    [Cooldown(10, 60, CooldownBucketType.User)]
    public Task RollDie(CommandContext context, int sides = 6)
    {
        int rolledNumber = _rand.Next(1, sides + 1);

        var message = new DiscordMessageBuilder()
            .WithEmbed(EmbedGenerator.Primary($"You rolled {rolledNumber}"))
            .WithReply(context.Message.Id, true);
        return context.RespondAsync(message);
    }

    [Command("Speak")]
    [Description("Get the bot to post the given message in a channel.")]
    [RequireUserPermissions(Permissions.Administrator)]
    [Cooldown(1, 60, CooldownBucketType.Guild)]
    public Task Speak(CommandContext context, DiscordChannel channel, string title, string content, string footerContent = "")
    {
        if (!context.Guild.Channels.ContainsKey(channel.Id))
        {
            _logger.LogWarning("Invalid Speak command request, the specified channel {ChannelId} does not exist", channel.Id);
            return context.RespondAsync(embed: EmbedGenerator.Error("The channel specified does not exist in this server."));
        }

        if (channel.Type != ChannelType.Text)
        {
            _logger.LogWarning("Invalid Speak command request, the specified channel {ChannelId} is not a text channel", channel.Id);
            return context.RespondAsync(embed: EmbedGenerator.Error("The channel specified is not a text channel."));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            _logger.LogWarning("Invalid Speak command request, no message was provided");
            return context.RespondAsync(embed: EmbedGenerator.Error("No valid message title was provided."));
        }

        return string.IsNullOrWhiteSpace(content)
            ? context.RespondAsync(embed: EmbedGenerator.Error("No valid message content was provided."))
            : (Task)channel.SendMessageAsync(embed: EmbedGenerator.Info(content, title, footerContent));
    }

    [Command("shutdown")]
    [Description("Gracefully shuts down the bot")]
    [RequireOwner]
    public async Task Shutdown(CommandContext context)
    {
        const string shutdownGif = "https://tenor.com/view/serio-no-nop-robot-robot-down-gif-12270251";
        if (await InteractivityHelper.GetConfirmation(context, "Bot Shutdown"))
        {
            await context.RespondAsync(EmbedGenerator.Info("Shutting down...", "Confirmed"));
            await context.Channel.SendMessageAsync(shutdownGif);
            _applicationLifetime.StopApplication();
        }
    }

    [Command("logs")]
    [Description("Send the current log file.")]
    [RequireOwner]
    public async Task GetLogs(CommandContext context)
    {
        string logDirectoryPath = Path.Combine(_appConfigurationService.BasePath, "Logs");
        var logDirectory = new DirectoryInfo(logDirectoryPath);

        var latestLogFile = logDirectory.GetFiles().MaxBy(x => x.LastWriteTimeUtc);

        if (latestLogFile != null)
        {
            using (var stream = File.Open(latestLogFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var fs = new MemoryStream())
                {
                    await stream.CopyToAsync(fs);
                    fs.Position = 0;
                    var message = new DiscordMessageBuilder().WithFile(latestLogFile.Name, fs);
                    await context.RespondAsync(message);
                    return;
                }
            }
        }

        await context.RespondAsync(EmbedGenerator.Warning("Something went wrong and I couldn't find the latest log file."));
    }
}