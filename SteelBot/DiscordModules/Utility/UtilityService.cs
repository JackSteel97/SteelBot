using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteelBot.DataProviders;
using SteelBot.Helpers;
using SteelBot.Responders;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Utility;

public class UtilityService
{
    private readonly AppConfigurationService _appConfigurationService;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly DataCache _cache;
    private readonly ILogger<UtilityService> _logger;
    private readonly Random _rand;

    public UtilityService(DataCache cache, AppConfigurationService appConfigurationService, IHostApplicationLifetime applicationLifetime, ILogger<UtilityService> logger)
    {
        _cache = cache;
        _rand = new Random();
        _appConfigurationService = appConfigurationService;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
    }

    public void ChannelsInfo(DiscordGuild discordGuild, IResponder responder)
    {
        if (!_cache.Guilds.TryGetGuild(discordGuild.Id, out var guild))
        {
            _logger.LogWarning("Guild {GuildId} is not tracked so the request to get channels info failed", discordGuild.Id);
            responder.Respond(new DiscordMessageBuilder().AddEmbed(EmbedGenerator.Error("Something went wrong and I couldn't get this Server's channel info, please try again later.")));
            return;
        }

        var pages = PaginationHelper.GenerateEmbedPages(new DiscordEmbedBuilder().WithColor(EmbedGenerator.InfoColour)
            .WithTitle($"{discordGuild.Name} Channels"), discordGuild.Channels.Values.Where(x => !x.IsCategory).OrderBy(x => x.Position), 5, (output, channel, index) =>
        {
            output.AppendLine(channel.Mention)
                .AppendLine($"{Formatter.Bold("Id")}: {Formatter.InlineCode(channel.Id.ToString())}")
                .AppendLine($"{Formatter.Bold("Type")}: {Formatter.InlineCode(channel.Type.ToString())}")
                .AppendLine($"{Formatter.Bold("Created")}: {Formatter.InlineCode(channel.CreationTimestamp.ToString("g"))}");

            if (channel.Bitrate.HasValue) output.AppendLine($"{Formatter.Bold("Bitrate")}: {Formatter.InlineCode($"{channel.Bitrate / 1000}kbps")}");

            output.AppendLine();
            return output;
        });

        responder.RespondPaginated(pages);
    }

    public void ServerInfo(DiscordGuild discordGuild, IResponder responder)
    {
        if (!_cache.Guilds.TryGetGuild(discordGuild.Id, out var guild))
        {
            _logger.LogWarning("Guild {GuildId} is not tracked so the request to get server info failed", discordGuild.Id);
            responder.Respond(new DiscordMessageBuilder().AddEmbed(EmbedGenerator.Error("Something went wrong and I couldn't get this Server's info, please try again later.")));
        }

        int totalUsers = discordGuild.MemberCount;
        int totalRoles = discordGuild.Roles.Count;
        int textChannels = 0, voiceChannels = 0, categories = 0;
        foreach (var channel in discordGuild.Channels)
            if (channel.Value.Type == ChannelType.Text)
                ++textChannels;
            else if (channel.Value.Type == ChannelType.Voice)
                ++voiceChannels;
            else if (channel.Value.Type == ChannelType.Category) ++categories;

        string created = (discordGuild.CreationTimestamp - DateTime.UtcNow).Humanize(2, maxUnit: TimeUnit.Year);
        string botAdded = (guild.BotAddedTo - DateTime.UtcNow).Humanize(2, maxUnit: TimeUnit.Year);
        var levelAnnouncementChannel = guild.GetLevelAnnouncementChannel(discordGuild);

        int rankRolesCount = 0;
        int selfRolesCount = 0;
        int triggersCount = 0;

        if (_cache.RankRoles.TryGetGuildRankRoles(guild.DiscordId, out var rankRoles)) rankRolesCount = rankRoles.Count;

        if (_cache.SelfRoles.TryGetGuildRoles(guild.DiscordId, out var selfRoles)) selfRolesCount = selfRoles.Count;

        if (_cache.Triggers.TryGetGuildTriggers(guild.DiscordId, out var triggers)) triggersCount = triggers.Count;

        var builder = new DiscordEmbedBuilder().WithColor(EmbedGenerator.InfoColour)
            .WithTitle($"{discordGuild.Name} Info")
            .AddField("Total Users", Formatter.InlineCode(totalUsers.ToString()), true)
            .AddField("Roles", Formatter.InlineCode(totalRoles.ToString()), true)
            .AddField("Text Channels", Formatter.InlineCode(textChannels.ToString()), true)
            .AddField("Voice Channels", Formatter.InlineCode(voiceChannels.ToString()), true)
            .AddField("Categories", Formatter.InlineCode(categories.ToString()), true)
            .AddField("System Channel", discordGuild.SystemChannel?.Mention ?? "`None Set`", true)
            .AddField("AFK Timeout", Formatter.InlineCode(TimeSpan.FromSeconds(discordGuild.AfkTimeout).Humanize()), true)
            .AddField("AFK Channel", discordGuild.AfkChannel?.Mention ?? "`None Set`", true)
            .AddField("Created", Formatter.InlineCode($"{created} ago ({discordGuild.CreationTimestamp:dd-MMM-yyyy HH:mm})"), true)
            .AddField("Guild Id", Formatter.InlineCode(guild.DiscordId.ToString()), true)
            .AddField("Verification Level", Formatter.InlineCode(discordGuild.VerificationLevel.ToString()), true)
            .AddField("Owner", discordGuild.Owner.Mention, true)
            .AddField("SteelBot Added", Formatter.InlineCode($"{botAdded} ago ({guild.BotAddedTo:dd-MMM-yyyy HH:mm})"), true)
            .AddField("Announcement Channel", levelAnnouncementChannel?.Mention ?? "`None Set`", true)
            .AddField("Rank Roles", Formatter.InlineCode(rankRolesCount.ToString()), true)
            .AddField("Self Roles", Formatter.InlineCode(selfRolesCount.ToString()), true)
            .AddField("Triggers", Formatter.InlineCode(triggersCount.ToString()), true)
            .AddField("Good Bot Votes", Formatter.InlineCode(guild.GoodBotVotes.ToString()), true)
            .AddField("Bad Bot Votes", Formatter.InlineCode(guild.BadBotVotes.ToString()), true)
            .WithThumbnail(discordGuild.IconUrl);

        responder.Respond(new DiscordMessageBuilder().AddEmbed(builder.Build()));
    }

    public void BotStatus(DateTimeOffset commandCreationTime, DiscordClient client, IResponder responder)
    {
        var now = DateTime.UtcNow;
        var uptime = now - _appConfigurationService.StartUpTime;
        var ping = now - commandCreationTime;

        var builder = new DiscordEmbedBuilder().WithColor(EmbedGenerator.InfoColour)
            .WithTitle("Bot Status")
            .AddField("Uptime", Formatter.InlineCode(uptime.Humanize(3)))
            .AddField("Processed Commands", Formatter.InlineCode(_appConfigurationService.HandledCommands.ToString()))
            .AddField("You -> Discord -> Bot Ping", Formatter.InlineCode(ping.Humanize()))
            .AddField("Bot -> Discord Ping", Formatter.InlineCode(TimeSpan.FromMilliseconds(client.Ping).Humanize()))
            .AddField("Version", Formatter.InlineCode(_appConfigurationService.Version));

        responder.Respond(new DiscordMessageBuilder().AddEmbed(builder.Build()));
    }

    public void Ping(IResponder responder)
    {
        string ret = DateTime.UtcNow.Millisecond % 5 == 0 ? "POG!" : "PONG!";
        responder.Respond(new DiscordMessageBuilder().AddEmbed(EmbedGenerator.Primary("", ret)));
    }

    public void Choose(IResponder responder, long numberToSelect, string[] options)
    {
        // Validation.
        if (numberToSelect <= 0)
        {
            _logger.LogWarning("Invalid Choose command request, the number to select {NumberToSelect} is less than zero", numberToSelect);
            responder.Respond(new DiscordMessageBuilder().AddEmbed(EmbedGenerator.Error("X must be greater than zero.")));
            return;
        }

        if (options.Length == 0)
        {
            _logger.LogWarning("Invalid Choose command request, no options were provider");
            responder.Respond(new DiscordMessageBuilder().AddEmbed(EmbedGenerator.Error("No options were provided.")));
            return;
        }

        if (numberToSelect > options.Length)
        {
            _logger.LogWarning("Invalid Choose command request, options provided {OptionsAmount} are less than the amount to select {NumberToSelect}", options.Length, numberToSelect);
            responder.Respond(new DiscordMessageBuilder().AddEmbed(
                EmbedGenerator.Error($"There are not enough options to choose {numberToSelect} unique options.\nPlease provide more options or choose less.")));
            return;
        }

        var remainingOptions = options.ToList();
        var selectedOptions = new List<string>((int)numberToSelect);
        for (int i = 0; i < numberToSelect; i++)
        {
            // Pick random option.
            int randIndex = _rand.Next(remainingOptions.Count);
            selectedOptions.Add(remainingOptions[randIndex]);
            // Remove from possible options.
            remainingOptions.RemoveAt(randIndex);
        }

        var message = new DiscordMessageBuilder()
            .WithEmbed(EmbedGenerator.Primary(string.Join(", ", selectedOptions), $"Chosen Option{(numberToSelect > 1 ? "s" : "")}"));
        responder.Respond(message);
    }

    public void FlipCoin(IResponder responder)
    {
        int side = _rand.Next(100);
        string result = "Heads!";
        if (side < 50) result = "Tails!";

        var message = new DiscordMessageBuilder()
            .WithEmbed(EmbedGenerator.Primary(result));
        responder.Respond(message);
    }

    public void RollDie(IResponder responder, int sides)
    {
        int rolledNumber = _rand.Next(1, sides + 1);

        var message = new DiscordMessageBuilder()
            .WithEmbed(EmbedGenerator.Primary($"You rolled {rolledNumber}"));
        responder.Respond(message);
    }

    public Task Speak(IResponder responder, DiscordGuild guild, DiscordChannel channel, string title, string content, string footerContent)
    {
        if (!guild.Channels.ContainsKey(channel.Id))
        {
            _logger.LogWarning("Invalid Speak command request, the specified channel {ChannelId} does not exist", channel.Id);
            return responder.RespondAsync(new DiscordMessageBuilder().WithEmbed(EmbedGenerator.Error("The channel specified does not exist in this server.")));
        }

        if (channel.Type != ChannelType.Text)
        {
            _logger.LogWarning("Invalid Speak command request, the specified channel {ChannelId} is not a text channel", channel.Id);
            return responder.RespondAsync(new DiscordMessageBuilder().WithEmbed(EmbedGenerator.Error("The channel specified is not a text channel.")));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            _logger.LogWarning("Invalid Speak command request, no message was provided");
            responder.RespondAsync(new DiscordMessageBuilder().WithEmbed(EmbedGenerator.Error("No valid message title was provided.")));
        }

        return string.IsNullOrWhiteSpace(content)
            ? responder.RespondAsync(new DiscordMessageBuilder().WithEmbed(EmbedGenerator.Error("No valid message content was provided.")))
            : (Task)channel.SendMessageAsync(EmbedGenerator.Info(content, title, footerContent));
    }

    public async Task Shutdown(IResponder responder, DiscordMember member)
    {
        const string shutdownGif = "https://tenor.com/view/serio-no-nop-robot-robot-down-gif-12270251";
        if (await InteractivityHelper.GetConfirmation(responder, member, "Bot Shutdown"))
        {
            await responder.RespondAsync(new DiscordMessageBuilder().WithContent(shutdownGif));
            _applicationLifetime.StopApplication();
        }
    }

    public async Task GetLogs(IResponder responder)
    {
        string logDirectoryPath = Path.Combine(_appConfigurationService.BasePath, "Logs");
        var logDirectory = new DirectoryInfo(logDirectoryPath);

        var latestLogFile = logDirectory.GetFiles().MaxBy(x => x.LastWriteTimeUtc);

        if (latestLogFile != null)
            await using (var stream = File.Open(latestLogFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var fs = new MemoryStream())
                {
                    await stream.CopyToAsync(fs);
                    fs.Position = 0;
                    var message = new DiscordMessageBuilder().AddFile(latestLogFile.Name, fs);
                    responder.Respond(message);
                    return;
                }
            }

        responder.Respond(new DiscordMessageBuilder().AddEmbed(EmbedGenerator.Warning("Something went wrong and I couldn't find the latest log file.")));
    }
}