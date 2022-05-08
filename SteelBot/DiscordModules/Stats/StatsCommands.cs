using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Humanizer;
using ScottPlot;
using SteelBot.Database.Models;
using SteelBot.Database.Models.Users;
using SteelBot.DiscordModules.Stats.Models;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Stats
{
    [Group("Stats")]
    [Description("Commands for viewing user stats and levels")]
    [RequireGuild]
    public class StatsCommands : TypingCommandModule
    {
        private readonly HashSet<string> AllowedMetrics = new() { "xp", "level", "message count", "message length", "afk", "voice", "muted", "deafened", "last active", "stream", "video" };
        private readonly DataHelpers DataHelper;
        private readonly LevelCardGenerator LevelCardGenerator;
        private readonly AppConfigurationService AppConfigurationService;
        private readonly UserLockingService UserLockingService;
        private readonly ErrorHandlingService ErrorHandlingService;

        public StatsCommands(DataHelpers dataHelpers, LevelCardGenerator levelCardGenerator, AppConfigurationService appConfigurationService, UserLockingService userLockingService, ErrorHandlingService errorHandlingService)
        {
            DataHelper = dataHelpers;
            LevelCardGenerator = levelCardGenerator;
            AppConfigurationService = appConfigurationService;
            UserLockingService = userLockingService;
            ErrorHandlingService = errorHandlingService;
        }

        [Command("commands")]
        [Description("Displays statistics about what commands are used most globally.")]
        [Cooldown(3, 60, CooldownBucketType.Global)]
        [RequireOwner]
        public async Task CommandStatistics(CommandContext context)
        {
            const string imageName = "commandstats.png";
            List<CommandStatistic> allStats = DataHelper.Stats.GetCommandStatistics();

            // Sort descending.
            allStats.Sort((cs1, cs2) => cs2.UsageCount.CompareTo(cs1.UsageCount));

            int barCount = allStats.Count;

            var plt = new ScottPlot.Plot(1280, 720);
            plt.Style(Style.Gray1);

            string[] labels = allStats.ConvertAll(s => s.CommandName).ToArray();
            double[] yData = allStats.ConvertAll(s => (double)s.UsageCount).ToArray();

            ScottPlot.Plottable.BarPlot bar = plt.AddBar(yData);
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
            using (await UserLockingService.ReaderLockAsync(discordUser.Guild.Id, discordUser.Id))
            {
                if (!DataHelper.Stats.TryGetUser(context.Guild.Id, discordUser.Id, out User user))
                {
                    context.RespondAsync(embed: EmbedGenerator.Error("I could not find their stats, are they new here?")).FireAndForget(ErrorHandlingService);
                    return;
                }

                DiscordEmbedBuilder embedBuilder = DataHelper.Stats.GetStatsEmbed(user, discordUser.Username);
                using (MemoryStream imageStream = await LevelCardGenerator.GenerateCard(user, discordUser))
                {
                    string fileName = $"{user.DiscordId}_stats.png";
                    DiscordMessageBuilder message = new DiscordMessageBuilder()
                        .WithFile(fileName, imageStream)
                        .WithEmbed(embedBuilder.WithImageUrl($"attachment://{fileName}").Build());
                    context.RespondAsync(message).FireAndForget(ErrorHandlingService);
                }
            }
        }

        [Command("me")]
        [Aliases("mine")]
        [Description("Displays your user statistics for this server.")]
        [Cooldown(3, 30, CooldownBucketType.User)]
        public async Task MyStats(CommandContext context)
        {
            using (await UserLockingService.ReaderLockAsync(context.Guild.Id, context.User.Id))
            {
                if (!DataHelper.Stats.TryGetUser(context.Guild.Id, context.Member.Id, out User user))
                {
                    context.RespondAsync(embed: EmbedGenerator.Error("I could not find your stats, are you new here?")).FireAndForget(ErrorHandlingService);
                    return;
                }

                DiscordEmbedBuilder embedBuilder = DataHelper.Stats.GetStatsEmbed(user, context.Member.Username);

                using (MemoryStream imageStream = await LevelCardGenerator.GenerateCard(user, context.Member))
                {
                    string fileName = $"{user.DiscordId}_stats.png";
                    DiscordMessageBuilder message = new DiscordMessageBuilder()
                        .WithFile(fileName, imageStream)
                        .WithEmbed(embedBuilder.WithImageUrl($"attachment://{fileName}").Build());
                    context.RespondAsync(message).FireAndForget(ErrorHandlingService);
                }
            }
        }

        [Command("breakdown")]
        [Description("Displays a breakdown of the given user (or your own) XP values.")]
        [RequireUserPermissions(Permissions.Administrator)]
        [Cooldown(2, 30, CooldownBucketType.User)]
        public async Task StatsBreakdown(CommandContext context, DiscordMember discordUser = null)
        {
            if(discordUser == null)
            {
                discordUser = context.Member;
            }

            using (await UserLockingService.ReaderLockAsync(discordUser.Guild.Id, discordUser.Id))
            {
                ulong userId = discordUser.Id;

                if (!DataHelper.Stats.TryGetUser(context.Guild.Id, userId, out User user))
                {
                    context.RespondAsync(embed: EmbedGenerator.Error("I could not find their stats, are they new here?")).FireAndForget(ErrorHandlingService);
                    return;
                }

                DiscordMember memberUser = await context.Guild.GetMemberAsync(userId);

                StringBuilder builder = new StringBuilder()
                    .Append(Formatter.Bold("Voice ")).AppendLine(Formatter.InlineCode(user.VoiceXpEarned.ToString("N0")))
                    .Append(Formatter.Bold("Streaming ")).AppendLine(Formatter.InlineCode(user.StreamingXpEarned.ToString("N0")))
                    .Append(Formatter.Bold("Video ")).AppendLine(Formatter.InlineCode(user.VideoXpEarned.ToString("N0")))
                    .Append(Formatter.Bold("Muted ")).AppendLine(Formatter.InlineCode($"-{user.MutedXpEarned:N0}"))
                    .Append(Formatter.Bold("Deafened ")).AppendLine(Formatter.InlineCode($"-{user.DeafenedXpEarned:N0}"))
                    .Append(Formatter.Bold("Messages ")).AppendLine(Formatter.InlineCode(user.MessageXpEarned.ToString("N0")))
                    .Append(Formatter.Bold("Offline ")).AppendLine(Formatter.InlineCode(user.DisconnectedXpEarned.ToString("N0")))
                    .AppendLine()
                    .Append(Formatter.Bold("Total ")).AppendLine(Formatter.InlineCode(user.TotalXp.ToString("N0")));

                DiscordEmbed embed = EmbedGenerator.Info(builder.ToString(), $"{memberUser.DisplayName} XP Breakdown");
                DiscordMessageBuilder message = new DiscordMessageBuilder().WithEmbed(embed).WithReply(context.Message.Id, mention: true);

                context.RespondAsync(message).FireAndForget(ErrorHandlingService);
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
                context.RespondAsync(embed: EmbedGenerator.Warning($"Missing metric parameter.\nAvailable Metrics are: {string.Join(", ", AllowedMetrics.Select(m => Formatter.InlineCode(m.Transform(To.TitleCase))))}"))
                    .FireAndForget(ErrorHandlingService);
                return;
            }
            if (!AllowedMetrics.Contains(metric))
            {
                context.RespondAsync(embed: EmbedGenerator.Warning($"Invalid metric: {Formatter.Bold(metric)}\nAvailable Metrics are: {string.Join(", ", AllowedMetrics.Select(m => Formatter.InlineCode(m.Transform(To.TitleCase))))}"))
                    .FireAndForget(ErrorHandlingService);
                return;
            }

            string[] metricValues;
            User[] orderedUsers;
            using (await UserLockingService.ReadLockAllUsersAsync(context.Guild.Id))
            {
                List<User> guildUsers = DataHelper.Stats.GetUsersInGuild(context.Guild.Id);
                if (guildUsers.Count == 0)
                {
                    context.RespondAsync(embed: EmbedGenerator.Warning("There are no users with statistics in this server yet."))
                        .FireAndForget(ErrorHandlingService);
                    return;
                }


                switch (metric.ToLower())
                {
                    case "xp":
                        orderedUsers = guildUsers.Where(u => u.TotalXp > 0).OrderByDescending(u => u.TotalXp).Take(top).ToArray();
                        metricValues = Array.ConvertAll(orderedUsers, u => $"XP: `{u.TotalXp:N0}`");
                        break;

                    case "level":
                        orderedUsers = guildUsers.Where(u => u.CurrentLevel > 0).OrderByDescending(u => u.CurrentLevel).Take(top).ToArray();
                        metricValues = Array.ConvertAll(orderedUsers, u => $"Level: `{u.CurrentLevel}`");
                        break;

                    case "message count":
                        orderedUsers = guildUsers.Where(u => u.MessageCount > 0).OrderByDescending(u => u.MessageCount).Take(top).ToArray();
                        metricValues = Array.ConvertAll(orderedUsers, u => $"Message Count: `{u.MessageCount:N0}`");
                        break;

                    case "message length":
                        orderedUsers = guildUsers.Where(u => u.GetAverageMessageLength() > 0).OrderByDescending(u => u.GetAverageMessageLength()).Take(top).ToArray();
                        metricValues = Array.ConvertAll(orderedUsers, u => $"Average Message Length: `{u.GetAverageMessageLength()} Characters`");
                        break;

                    case "afk":
                        orderedUsers = guildUsers.Where(u => u.TimeSpentAfk > TimeSpan.Zero).OrderByDescending(u => u.TimeSpentAfk).Take(top).ToArray();
                        metricValues = Array.ConvertAll(orderedUsers, u => $"AFK Time: `{u.TimeSpentAfk.Humanize(3)}`");
                        break;

                    case "voice":
                        orderedUsers = guildUsers.Where(u => u.TimeSpentInVoice > TimeSpan.Zero).OrderByDescending(u => u.TimeSpentInVoice).Take(top).ToArray();
                        metricValues = Array.ConvertAll(orderedUsers, u => $"Voice Time: `{u.TimeSpentInVoice.Humanize(3)}`");
                        break;

                    case "muted":
                        orderedUsers = guildUsers.Where(u => u.TimeSpentMuted > TimeSpan.Zero).OrderByDescending(u => u.TimeSpentMuted).Take(top).ToArray();
                        metricValues = Array.ConvertAll(orderedUsers, u => $"Muted Time: `{u.TimeSpentMuted.Humanize(3)}`");
                        break;

                    case "deafened":
                        orderedUsers = guildUsers.Where(u => u.TimeSpentDeafened > TimeSpan.Zero).OrderByDescending(u => u.TimeSpentDeafened).Take(top).ToArray();
                        metricValues = Array.ConvertAll(orderedUsers, u => $"Deafened Time: `{u.TimeSpentDeafened.Humanize(3)}`");
                        break;

                    case "last active":
                        orderedUsers = guildUsers.Where(u => u.LastActivity != default).OrderByDescending(u => u.LastActivity).Take(top).ToArray();
                        metricValues = Array.ConvertAll(orderedUsers, u => $"Last Active: `{u.LastActivity.Humanize()}`");
                        break;

                    case "stream":
                        orderedUsers = guildUsers.Where(u => u.TimeSpentStreaming > TimeSpan.Zero).OrderByDescending(u => u.TimeSpentStreaming).Take(top).ToArray();
                        metricValues = Array.ConvertAll(orderedUsers, u => $"Streaming Time: `{u.TimeSpentStreaming.Humanize(3)}`");
                        break;

                    case "video":
                        orderedUsers = guildUsers.Where(u => u.TimeSpentOnVideo > TimeSpan.Zero).OrderByDescending(u => u.TimeSpentOnVideo).Take(top).ToArray();
                        metricValues = Array.ConvertAll(orderedUsers, u => $"Video Time: `{u.TimeSpentOnVideo.Humanize(3)}`");
                        break;

                    default:
                        context.RespondAsync(embed: EmbedGenerator.Error("Invalid Metric selected.")).FireAndForget(ErrorHandlingService);
                        return;
                }
            }


            if (orderedUsers.Length == 0)
            {
                context.RespondAsync(EmbedGenerator.Info("There are no entries to show")).FireAndForget(ErrorHandlingService);
                return;
            }

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithColor(EmbedGenerator.InfoColour)
                .WithTitle($"{context.Guild.Name} {metric.Transform(To.TitleCase)} Leaderboard")
                .WithTimestamp(DateTime.UtcNow);

            List<Page> pages = PaginationHelper.GenerateEmbedPages(embedBuilder, orderedUsers, 5, (builder, user, index) =>
            {
                return builder
                    .Append("**").Append((index + 1).Ordinalize()).Append("** - ").AppendLine(user.DiscordId.ToUserMention())
                    .AppendLine(metricValues[index]);
            });

            InteractivityExtension interactivity = context.Client.GetInteractivity();

            await interactivity.SendPaginatedMessageAsync(context.Channel, context.User, pages);
        }

        [Command("Leaderboard")]
        [Description("Displays a leaderboard of levels for this server.")]
        [Cooldown(2, 60, CooldownBucketType.Channel)]
        public async Task LevelsLeaderboard(CommandContext context, int top = 100)
        {
            if (top <= 0)
            {
                context.RespondAsync(embed: EmbedGenerator.Warning("You cannot get a leaderboard with no entries.")).FireAndForget(ErrorHandlingService);
                return;
            }

            List<Page> pages;
            using (await UserLockingService.ReadLockAllUsersAsync(context.Guild.Id))
            {
                List<User> guildUsers = DataHelper.Stats.GetUsersInGuild(context.Guild.Id);
                if (guildUsers.Count == 0)
                {
                    context.RespondAsync(embed: EmbedGenerator.Warning("There are no users with statistics in this server yet.")).FireAndForget(ErrorHandlingService);
                    return;
                }

                User[] orderedByXp = guildUsers.Where(u => u.TotalXp > 0).OrderByDescending(u => u.TotalXp).Take(top).ToArray();

                if (orderedByXp.Length == 0)
                {
                    context.RespondAsync(EmbedGenerator.Info("There are no entries to show")).FireAndForget(ErrorHandlingService);
                    return;
                }

                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(EmbedGenerator.InfoColour)
                    .WithTitle($"{context.Guild.Name} Leaderboard")
                    .WithTimestamp(DateTime.UtcNow);

                pages = PaginationHelper.GenerateEmbedPages(embedBuilder, orderedByXp, 5, (builder, user, index) =>
                {
                    return builder
                        .Append("**").Append((index + 1).Ordinalize()).Append("** - ").AppendLine(user.DiscordId.ToUserMention())
                        .Append("Level `").Append(user.CurrentLevel).AppendLine("`")
                        .Append("XP `").Append(user.TotalXp.ToString("N0")).AppendLine("`");
                });
            }

            InteractivityExtension interactivity = context.Client.GetInteractivity();

            await interactivity.SendPaginatedMessageAsync(context.Channel, context.User, pages);
        }

        [Command("All")]
        [Description("Displays the leaderboard but with all stats detail for each user.")]
        [Cooldown(1, 60, CooldownBucketType.Channel)]
        public async Task AllStats(CommandContext context, int top = 10)
        {
            if (top <= 0)
            {
                context.RespondAsync(embed: EmbedGenerator.Warning("You cannot get a leaderboard with no entries.")).FireAndForget(ErrorHandlingService);
                return;
            }

            List<Page> pages;
            using (await UserLockingService.ReadLockAllUsersAsync(context.Guild.Id))
            {
                List<User> guildUsers = DataHelper.Stats.GetUsersInGuild(context.Guild.Id);
                if (guildUsers.Count == 0)
                {
                    context.RespondAsync(embed: EmbedGenerator.Warning("There are no users with statistics in this server yet.")).FireAndForget(ErrorHandlingService);
                    return;
                }
                if (top > guildUsers.Count)
                {
                    top = guildUsers.Count;
                }

                // Sort by xp.
                guildUsers.Sort((u1, u2) => u2.TotalXp.CompareTo(u1.TotalXp));

                // Get top x.
                List<User> orderedByXp = guildUsers.GetRange(0, top);

                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                   .WithColor(EmbedGenerator.InfoColour)
                   .WithTitle($"{context.Guild.Name} Leaderboard")
                   .WithTimestamp(DateTime.Now);

                pages = PaginationHelper.GenerateEmbedPages(embedBuilder, orderedByXp, 2, (builder, user, index) =>
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
                });
            }

            InteractivityExtension interactivity = context.Client.GetInteractivity();
            await interactivity.SendPaginatedMessageAsync(context.Channel, context.User, pages);
        }

        [Command("Velocity")]
        [Aliases("Gains")]
        [Description("Show the current XP velocity for yourself or a given user")]
        [Cooldown(5, 60, CooldownBucketType.Channel)]
        [RequirePermissions(Permissions.Administrator)]
        public async Task Velocity(CommandContext context, DiscordMember target = null)
        {
            target ??= context.Member;

            XpVelocity velocity;
            using (await UserLockingService.ReaderLockAsync(target.Guild.Id, target.Id))
            {
                var availablePets = DataHelper.Pets.GetAvailablePets(target.Guild.Id, target.Id, out _);
                velocity = DataHelper.Stats.GetVelocity(target, availablePets);
            }

            var embed = new DiscordEmbedBuilder().WithColor(EmbedGenerator.InfoColour)
                .WithTitle($"{target.DisplayName} XP Velocity")
                .WithDescription("XP earned per unit for each action")
                .WithTimestamp(DateTime.Now)
                .AddField("Message", Formatter.InlineCode(velocity.Message.ToString("N0")), true)
                .AddField("Voice", Formatter.InlineCode(velocity.Voice.ToString("N0")), true)
                .AddField("Muted", Formatter.InlineCode($"-{velocity.Muted:N0}"), true)
                .AddField("Deafened", Formatter.InlineCode($"-{velocity.Deafened:N0}"))
                .AddField("Streaming", Formatter.InlineCode(velocity.Streaming.ToString("N0")), true)
                .AddField("Video", Formatter.InlineCode(velocity.Video.ToString("N0")), true)
                .AddField("Offline", Formatter.InlineCode(velocity.Passive.ToString("N0")), true);

            context.RespondAsync(embed).FireAndForget(ErrorHandlingService);
        }
    }
}