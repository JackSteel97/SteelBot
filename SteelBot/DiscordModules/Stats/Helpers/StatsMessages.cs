using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using SteelBot.Database.Models.Users;
using SteelBot.DiscordModules.Stats.Models;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using System;
using System.IO;
using System.Text;

namespace SteelBot.DiscordModules.Stats.Helpers;

public static class StatsMessages
{
    public static DiscordMessageBuilder UnableToFindUser()
    {
        var embed = EmbedGenerator.Error("I could not find that user, are they new here?");
        return new DiscordMessageBuilder().WithEmbed(embed);
    }

    public static DiscordMessageBuilder StatsBreakdown(User user, string displayName)
    {
        var builder = new StringBuilder()
            .Append(Formatter.Bold("Voice ")).AppendLine(Formatter.InlineCode(user.VoiceXpEarned.ToString("N0")))
            .Append(Formatter.Bold("Streaming ")).AppendLine(Formatter.InlineCode(user.StreamingXpEarned.ToString("N0")))
            .Append(Formatter.Bold("Video ")).AppendLine(Formatter.InlineCode(user.VideoXpEarned.ToString("N0")))
            .Append(Formatter.Bold("Muted ")).AppendLine(Formatter.InlineCode($"-{user.MutedXpEarned:N0}"))
            .Append(Formatter.Bold("Deafened ")).AppendLine(Formatter.InlineCode($"-{user.DeafenedXpEarned:N0}"))
            .Append(Formatter.Bold("Messages ")).AppendLine(Formatter.InlineCode(user.MessageXpEarned.ToString("N0")))
            .Append(Formatter.Bold("Offline ")).AppendLine(Formatter.InlineCode(user.DisconnectedXpEarned.ToString("N0")))
            .Append(Formatter.Bold("Active Streak ")).AppendLine(Formatter.InlineCode(user.ActivityStreakXpEarned.ToString("N0")))
            .AppendLine()
            .Append(Formatter.Bold("Total ")).AppendLine(Formatter.InlineCode(user.TotalXp.ToString("N0")));

        var embed = EmbedGenerator.Info(builder.ToString(), $"{displayName} XP Breakdown");
        return new DiscordMessageBuilder().WithEmbed(embed);
    }

    public static DiscordMessageBuilder StatsVelocity(XpVelocity velocity, string displayName)
    {
        var embed = new DiscordEmbedBuilder().WithColor(EmbedGenerator.InfoColour)
            .WithTitle($"{displayName} XP Velocity")
            .WithDescription("XP earned per unit for each action")
            .WithTimestamp(DateTime.Now)
            .AddField("Message", Formatter.InlineCode(velocity.Message.ToString("N0")), true)
            .AddField("Voice", Formatter.InlineCode(velocity.Voice.ToString("N0")), true)
            .AddField("Muted", Formatter.InlineCode($"-{velocity.Muted:N0}"), true)
            .AddField("Deafened", Formatter.InlineCode($"-{velocity.Deafened:N0}"), true)
            .AddField("Streaming", Formatter.InlineCode(velocity.Streaming.ToString("N0")), true)
            .AddField("Video", Formatter.InlineCode(velocity.Video.ToString("N0")), true)
            .AddField("Offline", Formatter.InlineCode(velocity.Passive.ToString("N0")), true);
        return new DiscordMessageBuilder().WithEmbed(embed);
    }

    public static DiscordEmbedBuilder GetStatsEmbed(User user, string username)
    {
        var embedBuilder = new DiscordEmbedBuilder()
            .WithColor(EmbedGenerator.InfoColour)
            .WithTitle($"{username} Stats")
            .AddField("Message Count", $"`{user.MessageCount:N0} Messages`", true)
            .AddField("Average Message Length", $"`{user.GetAverageMessageLength()} Characters`", true)
            .AddField("AFK Time", $"`{user.TimeSpentAfk.Humanize(2)}`", true)
            .AddField("Voice Time", $"`{user.TimeSpentInVoice.Humanize(2)} (100%)`", true)
            .AddField("Streaming Time", $"`{user.TimeSpentStreaming.Humanize(2)} ({MathsHelper.GetPercentageOfDuration(user.TimeSpentStreaming, user.TimeSpentInVoice):P2})`", true)
            .AddField("Video Time", $"`{user.TimeSpentOnVideo.Humanize(2)} ({MathsHelper.GetPercentageOfDuration(user.TimeSpentOnVideo, user.TimeSpentInVoice):P2})`", true)
            .AddField("Muted Time", $"`{user.TimeSpentMuted.Humanize(2)} ({MathsHelper.GetPercentageOfDuration(user.TimeSpentMuted, user.TimeSpentInVoice):P2})`", true)
            .AddField("Deafened Time", $"`{user.TimeSpentDeafened.Humanize(2)} ({MathsHelper.GetPercentageOfDuration(user.TimeSpentDeafened, user.TimeSpentInVoice):P2})`", true);

        return embedBuilder;
    }

    public static DiscordMessageBuilder StatsCard(User user, string username, MemoryStream imageStream)
    {
        var embedBuilder = GetStatsEmbed(user, username);

        string fileName = $"{user.DiscordId}_stats.png";
        return new DiscordMessageBuilder()
            .WithFile(fileName, imageStream)
            .WithEmbed(embedBuilder.WithImageUrl($"attachment://{fileName}").Build());
    }

    public static DiscordMessageBuilder NoEntriesLeaderboard()
    {
        return new DiscordMessageBuilder().WithEmbed(EmbedGenerator.Warning("You cannot get a leaderboard with no entries."));
    }

    public static DiscordMessageBuilder NoUsersWithStatistics()
    {
        return new DiscordMessageBuilder().WithEmbed(EmbedGenerator.Warning("There are no users with statistics in this server yet."));
    }

    public static DiscordEmbedBuilder LeaderboardBase(string guildName, string metric = null)
    {
        string metricInsert = string.Empty;
        if (!string.IsNullOrWhiteSpace(metric))
        {
            metricInsert = $" {metric.Transform(To.TitleCase)}";
        }
        
        return new DiscordEmbedBuilder()
            .WithColor(EmbedGenerator.InfoColour)
            .WithTitle($"{guildName}{metricInsert} Leaderboard")
            .WithTimestamp(DateTime.Now);
    }

    public static StringBuilder AppendUserStats(StringBuilder builder, User user, int index)
    {
        return builder
            .Append("**__").Append((index + 1).Ordinalize()).Append("__** - <@").Append(user.DiscordId).Append("> - **Level** `").Append(user.CurrentLevel).AppendLine("`")
            .AppendLine("**__Messages__**")
            .Append(EmojiConstants.Numbers.HashKeycap).Append(" - **Count** `").AppendFormat("{0:N0}", user.MessageCount).AppendLine("`")
            .Append(EmojiConstants.Objects.Ruler).Append(" - **Average Length** `").Append(user.GetAverageMessageLength()).AppendLine(" Characters`")
            .AppendLine("**__Durations__**")
            .Append(EmojiConstants.Objects.Microphone).Append(" - **Voice** `").Append(user.TimeSpentInVoice.Humanize(3)).AppendLine("`")
            .Append(EmojiConstants.Objects.Television).Append(" - **Streaming** `").Append(user.TimeSpentStreaming.Humanize(3)).AppendLine("`")
            .Append(EmojiConstants.Objects.Camera).Append(" - **Video** `").Append(user.TimeSpentOnVideo.Humanize(3)).AppendLine("`")
            .Append(EmojiConstants.Objects.MutedSpeaker).Append(" - **Muted** `").Append(user.TimeSpentMuted.Humanize(3)).AppendLine("`")
            .Append(EmojiConstants.Objects.BellWithSlash).Append(" - **Deafened** `").Append(user.TimeSpentDeafened.Humanize(3)).AppendLine("`")
            .Append(EmojiConstants.Symbols.Zzz).Append(" - **AFK** `").Append(user.TimeSpentAfk.Humanize(3)).AppendLine("`");
    }

    public static StringBuilder AppendUserMetric(StringBuilder builder, User user, int index, string[] metricValues)
    {
        return builder
            .Append("**").Append((index + 1).Ordinalize()).Append("** - ").AppendLine(user.DiscordId.ToUserMention())
            .AppendLine(metricValues[index]);
    }

    public static DiscordMessageBuilder NoEntriesToShow()
    {
        return new DiscordMessageBuilder().WithEmbed(EmbedGenerator.Info("There are no entries to show"));
    }

    public static StringBuilder AppendLeaderboardEntry(StringBuilder builder, User user, int index)
    {
        return builder
            .Append("**").Append((index + 1).Ordinalize()).Append("** - ").AppendLine(user.DiscordId.ToUserMention())
            .Append("Level `").Append(user.CurrentLevel).AppendLine("`")
            .Append("XP `").Append(user.TotalXp.ToString("N0")).AppendLine("`");
    }

    public static DiscordMessageBuilder MissingMetric()
    {
        return new DiscordMessageBuilder()
            .WithEmbed(EmbedGenerator.Warning($"Missing metric parameter.{Environment.NewLine}Available Metrics are: {AllowedMetrics.MetricsList}"));
    }

    public static DiscordMessageBuilder InvalidMetric(string invalidMetric)
    {
        return new DiscordMessageBuilder()
            .WithEmbed(EmbedGenerator.Warning($"Invalid metric: {Formatter.Bold(invalidMetric)}{Environment.NewLine}Available Metrics are: {AllowedMetrics.MetricsList}"));

    }
}