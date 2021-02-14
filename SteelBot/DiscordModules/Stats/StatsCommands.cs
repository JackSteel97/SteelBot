using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Humanizer;
using SteelBot.Database.Models;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Interactivity;
using ScottPlot;
using System.IO;
using ScottPlot.Drawing;
using System.Drawing;

namespace SteelBot.DiscordModules.Stats
{
    [Group("Stats")]
    [Description("Commands for viewing user stats and levels")]
    [RequireGuild]
    public class StatsCommands : TypingCommandModule
    {
        private readonly HashSet<string> AllowedMetrics = new HashSet<string>() { "xp", "level", "message count", "message length", "efficiency", "voice", "muted", "deafened", "last active", "stream", "video" };
        private readonly DataHelpers DataHelper;
        private readonly LevelCardGenerator LevelCardGenerator;

        public StatsCommands(DataHelpers dataHelpers, LevelCardGenerator levelCardGenerator)
        {
            DataHelper = dataHelpers;
            LevelCardGenerator = levelCardGenerator;
        }

        [Command("commands")]
        [Description("Displays statistics about what commands are used most globally.")]
        [Cooldown(3, 60, CooldownBucketType.Global)]
        [RequireOwner]
        public async Task CommandStatistics(CommandContext context)
        {
            const string imageName = "commandstats.png";
            var allStats = DataHelper.Stats.GetCommandStatistics();

            // Sort descending.
            allStats.Sort((cs1, cs2) => cs2.UsageCount.CompareTo(cs1.UsageCount));

            int barCount = allStats.Count;

            var plt = new ScottPlot.Plot(1280, 720);
            plt.Style(Style.Gray1);

            string[] labels = allStats.ConvertAll(s => s.CommandName).ToArray();
            double[] yData = allStats.ConvertAll(s => (double)s.UsageCount).ToArray();

            var bar = plt.AddBar(yData);
            bar.ShowValuesAboveBars = true;
            plt.SetAxisLimits(yMin: 0);
            plt.XTicks(labels);
            plt.XAxis.TickLabelStyle(Color.White, rotation: 20);
            plt.YAxis.TickLabelStyle(Color.White);
            plt.Grid(false);

            plt.Title("Command Usage");
            plt.YLabel("Usage Count");
            plt.SaveFig(imageName);

            using (FileStream imageStream = File.OpenRead(imageName))
            {
                DiscordMessageBuilder message = new DiscordMessageBuilder().WithFile(imageName, imageStream);
                await context.RespondAsync(message);
            }
            File.Delete(imageName);
        }

        [GroupCommand]
        [Description("Displays the given user's statistics for this server.")]
        [Cooldown(1, 30, CooldownBucketType.User)]
        public async Task TheirStats(CommandContext context, DiscordMember discordUser)
        {
            if (!DataHelper.Stats.TryGetUser(context.Guild.Id, discordUser.Id, out User user))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("I could not find their stats, are they new here?"));
                return;
            }

            DiscordEmbedBuilder embedBuilder = DataHelper.Stats.GetStatsEmbed(user, discordUser.Username);
            using (var imageStream = await LevelCardGenerator.GenerateCard(user, discordUser))
            {
                string fileName = $"{user.DiscordId}_stats.png";
                DiscordMessageBuilder message = new DiscordMessageBuilder()
                    .WithFile(fileName, imageStream)
                    .WithEmbed(embedBuilder.WithImageUrl($"attachment://{fileName}").Build());
                await context.RespondAsync(message);
            }
        }

        [Command("me")]
        [Description("Displays your user statistics for this server.")]
        [Cooldown(3, 30, CooldownBucketType.User)]
        public async Task MyStats(CommandContext context)
        {
            if (!DataHelper.Stats.TryGetUser(context.Guild.Id, context.Member.Id, out User user))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("I could not find your stats, are you new here?"));
                return;
            }

            DiscordEmbedBuilder embedBuilder = DataHelper.Stats.GetStatsEmbed(user, context.Member.Username);

            using (var imageStream = await LevelCardGenerator.GenerateCard(user, context.Member))
            {
                string fileName = $"{user.DiscordId}_stats.png";
                DiscordMessageBuilder message = new DiscordMessageBuilder()
                    .WithFile(fileName, imageStream)
                    .WithEmbed(embedBuilder.WithImageUrl($"attachment://{fileName}").Build());
                await context.RespondAsync(message);
            }
        }

        [GroupCommand]
        [Description("Displays the Top 50 leaderboard sorted by the given metric.")]
        [Cooldown(2, 60, CooldownBucketType.Channel)]
        public async Task MetricLeaderboard(CommandContext context, [RemainingText] string metric)
        {
            const int top = 50;
            if (string.IsNullOrWhiteSpace(metric))
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning($"Missing metric parameter.\nAvailable Metrics are: {string.Join(", ", AllowedMetrics.Select(m => Formatter.InlineCode(m.Transform(To.TitleCase))))}"));
                return;
            }
            if (!AllowedMetrics.Contains(metric))
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning($"Invalid metric: {Formatter.Bold(metric)}\nAvailable Metrics are: {string.Join(", ", AllowedMetrics.Select(m => Formatter.InlineCode(m.Transform(To.TitleCase))))}"));
                return;
            }
            List<User> guildUsers = DataHelper.Stats.GetUsersInGuild(context.Guild.Id);
            if (guildUsers.Count == 0)
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning("There are no users with statistics in this server yet."));
            }

            string[] metricValues;
            User[] orderedUsers;
            switch (metric.ToLower())
            {
                case "xp":
                    orderedUsers = guildUsers.OrderByDescending(u => u.TotalXp).Take(top).ToArray();
                    metricValues = Array.ConvertAll(orderedUsers, u => $"XP: `{u.TotalXp}`");
                    break;

                case "level":
                    orderedUsers = guildUsers.OrderByDescending(u => u.CurrentLevel).Take(top).ToArray();
                    metricValues = Array.ConvertAll(orderedUsers, u => $"Level: `{u.CurrentLevel}`");
                    break;

                case "message count":
                    orderedUsers = guildUsers.OrderByDescending(u => u.MessageCount).Take(top).ToArray();
                    metricValues = Array.ConvertAll(orderedUsers, u => $"Message Count: `{u.MessageCount}`");
                    break;

                case "message length":
                    orderedUsers = guildUsers.OrderByDescending(u => u.GetAverageMessageLength()).Take(top).ToArray();
                    metricValues = Array.ConvertAll(orderedUsers, u => $"Average Message Length: `{u.GetAverageMessageLength()} Characters`");
                    break;

                case "efficiency":
                    orderedUsers = guildUsers.OrderByDescending(u => u.GetMessageEfficiency()).Take(top).ToArray();
                    metricValues = Array.ConvertAll(orderedUsers, u => $"Message Efficiency: `{u.GetMessageEfficiency():P2}`");
                    break;

                case "voice":
                    orderedUsers = guildUsers.OrderByDescending(u => u.TimeSpentInVoice).Take(top).ToArray();
                    metricValues = Array.ConvertAll(orderedUsers, u => $"Voice Time: `{u.TimeSpentInVoice.Humanize(3)}`");
                    break;

                case "muted":
                    orderedUsers = guildUsers.OrderByDescending(u => u.TimeSpentMuted).Take(top).ToArray();
                    metricValues = Array.ConvertAll(orderedUsers, u => $"Muted Time: `{u.TimeSpentMuted.Humanize(3)}`");
                    break;

                case "deafened":
                    orderedUsers = guildUsers.OrderByDescending(u => u.TimeSpentDeafened).Take(top).ToArray();
                    metricValues = Array.ConvertAll(orderedUsers, u => $"Deafened Time: `{u.TimeSpentDeafened.Humanize(3)}`");
                    break;

                case "last active":
                    orderedUsers = guildUsers.OrderByDescending(u => u.LastActivity).Take(top).ToArray();
                    metricValues = Array.ConvertAll(orderedUsers, u => $"Last Active: `{u.LastActivity.Humanize()}`");
                    break;

                case "stream":
                    orderedUsers = guildUsers.OrderByDescending(u => u.TimeSpentStreaming).Take(top).ToArray();
                    metricValues = Array.ConvertAll(orderedUsers, u => $"Streaming Time: `{u.TimeSpentStreaming.Humanize(3)}`");
                    break;

                case "video":
                    orderedUsers = guildUsers.OrderByDescending(u => u.TimeSpentOnVideo).Take(top).ToArray();
                    metricValues = Array.ConvertAll(orderedUsers, u => $"Video Time: `{u.TimeSpentOnVideo.Humanize(3)}`");
                    break;

                default:
                    await context.RespondAsync(embed: EmbedGenerator.Error("Invalid Metric selected."));
                    return;
            }

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithColor(EmbedGenerator.InfoColour)
                .WithTitle($"{context.Guild.Name} {metric.Transform(To.TitleCase)} Leaderboard")
                .WithTimestamp(DateTime.UtcNow);

            StringBuilder leaderboard = new StringBuilder();
            for (int i = 0; i < orderedUsers.Length; i++)
            {
                User user = orderedUsers[i];
                leaderboard
                    .AppendLine($"**{(i + 1).Ordinalize()}** - <@{user.DiscordId}>")
                    .AppendLine(metricValues[i]);

                if (i != orderedUsers.Length - 1)
                {
                    leaderboard.AppendLine();
                }
            }

            var interactivity = context.Client.GetInteractivity();
            var leaderboardPages = interactivity.GeneratePagesInEmbed(leaderboard.ToString(), SplitType.Line, embedBuilder);

            await interactivity.SendPaginatedMessageAsync(context.Channel, context.User, leaderboardPages);
        }

        [Command("Leaderboard")]
        [Description("Displays a leaderboard of levels for this server.")]
        [Cooldown(2, 60, CooldownBucketType.Channel)]
        public async Task LevelsLeaderboard(CommandContext context, int top = 100)
        {
            if (top <= 0)
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning("You cannot get a leaderboard with no entries."));
                return;
            }

            List<User> guildUsers = DataHelper.Stats.GetUsersInGuild(context.Guild.Id);
            if (guildUsers.Count == 0)
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning("There are no users with statistics in this server yet."));
                return;
            }

            User[] orderedByXp = guildUsers.OrderByDescending(u => u.TotalXp).Take(top).ToArray();

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithColor(EmbedGenerator.InfoColour)
                .WithTitle($"{context.Guild.Name} Leaderboard")
                .WithTimestamp(DateTime.UtcNow);

            StringBuilder leaderboard = new StringBuilder();
            for (int i = 0; i < orderedByXp.Length; i++)
            {
                User user = orderedByXp[i];
                leaderboard
                    .AppendLine($"**{(i + 1).Ordinalize()}** - <@{user.DiscordId}>")
                    .AppendLine($"Level `{user.CurrentLevel}`")
                    .AppendLine($"XP `{$"{user.TotalXp:n0}"}`");

                if (i != orderedByXp.Length - 1)
                {
                    leaderboard.AppendLine();
                }
            }

            var interactivity = context.Client.GetInteractivity();
            var leaderboardPages = interactivity.GeneratePagesInEmbed(leaderboard.ToString(), SplitType.Line, embedBuilder);

            await interactivity.SendPaginatedMessageAsync(context.Channel, context.User, leaderboardPages);
        }

        [Command("All")]
        [Description("Displays the leaderboard but with all stats detail for each user.")]
        [Cooldown(1, 60, CooldownBucketType.Channel)]
        public async Task AllStats(CommandContext context, int top = 10)
        {
            if (top <= 0)
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning("You cannot get a leaderboard with no entries."));
                return;
            }

            List<User> guildUsers = DataHelper.Stats.GetUsersInGuild(context.Guild.Id);
            if (guildUsers.Count == 0)
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning("There are no users with statistics in this server yet."));
                return;
            }
            if (top > guildUsers.Count)
            {
                top = guildUsers.Count;
            }

            // Sort by xp.
            guildUsers.Sort((u1, u2) =>
            {
                return u2.TotalXp.CompareTo(u1.TotalXp);
            });
            // Get top x.
            List<User> orderedByXp = guildUsers.GetRange(0, top);

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
               .WithColor(EmbedGenerator.InfoColour)
               .WithTitle($"{context.Guild.Name} Leaderboard")
               .WithTimestamp(DateTime.UtcNow);

            StringBuilder leaderboard = new StringBuilder();
            int requiredPages = (int)Math.Ceiling((double)orderedByXp.Count / 2);

            List<Page> pages = new List<Page>(requiredPages);
            for (int i = 0; i < orderedByXp.Count; i++)
            {
                User user = orderedByXp[i];
                leaderboard
                    .AppendLine($"**__{(i + 1).Ordinalize()}__** - <@{user.DiscordId}> - **Level** `{user.CurrentLevel}`")
                    .AppendLine($"**__Messages__**")
                    .AppendLine($"{EmojiConstants.Numbers.HashKeycap} - **Count** `{user.MessageCount}`")
                    .AppendLine($"{EmojiConstants.Objects.LightBulb} - **Efficiency** {Formatter.InlineCode(user.GetMessageEfficiency().ToString("P2"))}")
                    .AppendLine($"{EmojiConstants.Objects.Ruler} - **Average Length** `{user.GetAverageMessageLength()} Characters`")
                    .AppendLine("**__Durations__**")
                    .AppendLine($"{EmojiConstants.Objects.Microphone} - **Voice** `{user.TimeSpentInVoice.Humanize(3)}`")
                    .AppendLine($"{EmojiConstants.Objects.Television} - **Streaming** `{user.TimeSpentStreaming.Humanize(3)}`")
                    .AppendLine($"{EmojiConstants.Objects.Camera} - **Video** `{user.TimeSpentOnVideo.Humanize(3)}`")
                    .AppendLine($"{EmojiConstants.Objects.MutedSpeaker} - **Muted** `{user.TimeSpentMuted.Humanize(3)}`")
                    .AppendLine($"{EmojiConstants.Objects.BellWithSlash} - **Deafened** `{user.TimeSpentDeafened.Humanize(3)}`");

                if (i != orderedByXp.Count - 1)
                {
                    leaderboard.AppendLine();
                }

                if (i % 2 == 1 || i == orderedByXp.Count - 1)
                {
                    // Manually start a new page every 2 users.
                    int currentPage = (i / 2) + 1;

                    DiscordEmbedBuilder embedPage = embedBuilder
                        .WithDescription(leaderboard.ToString())
                        .WithFooter($"Page {currentPage}/{requiredPages}");

                    pages.Add(new Page(embed: embedBuilder));
                    leaderboard.Clear();
                }
            }

            var interactivity = context.Client.GetInteractivity();

            await interactivity.SendPaginatedMessageAsync(context.Channel, context.User, pages);
        }
    }
}