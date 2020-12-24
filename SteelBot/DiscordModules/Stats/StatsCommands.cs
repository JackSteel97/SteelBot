using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Humanizer;
using SteelBot.Database.Models;
using SteelBot.Helpers;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Stats
{
    [Group("Stats")]
    [Description("Commands for viewing user stats and levels")]
    [RequireGuild]
    public class StatsCommands : BaseCommandModule
    {
        private readonly HashSet<string> AllowedMetrics = new HashSet<string>() { "xp", "level", "message count", "message length", "efficiency", "voice", "muted", "deafened" };
        private readonly DataHelpers DataHelper;
        private readonly LevelCardGenerator LevelCardGenerator;

        public StatsCommands(DataHelpers dataHelpers, LevelCardGenerator levelCardGenerator)
        {
            DataHelper = dataHelpers;
            LevelCardGenerator = levelCardGenerator;
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
                string fileName = $"{context.Member.Username}_stats.png";
                await context.RespondWithFileAsync(fileName, imageStream, embed: embedBuilder.WithImageUrl($"attachment://{fileName}").Build());
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
                string fileName = $"{context.Member.Username}_stats.png";
                await context.RespondWithFileAsync(fileName, imageStream, embed: embedBuilder.WithImageUrl($"attachment://{fileName}").Build());
            }
        }

        [GroupCommand]
        [Description("Displays the leaderboard sorted by the given metric.")]
        [Cooldown(2, 60, CooldownBucketType.Channel)]
        public async Task MetricLeaderboard(CommandContext context, [RemainingText] string metric)
        {
            const int top = 10;
            if (!AllowedMetrics.Contains(metric))
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning($"Invalid metric: {Formatter.Bold(metric)}\nAvailable Metrics are: {string.Join(", ", AllowedMetrics.Select(m => Formatter.InlineCode(m.Transform(To.TitleCase))))}"));
                return;
            }
            List<User> guildUsers = DataHelper.Stats.GetUsersInGuild(context.Guild.Id);
            if(guildUsers.Count == 0)
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
                    metricValues = Array.ConvertAll(orderedUsers, u => $"Message Efficiency: `{u.GetMessageEfficiency().ToString("P2")}`");
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
            for (int i = 0; i < orderedByXp.Count; i++)
            {
                User user = orderedByXp[i];

                leaderboard
                    .AppendLine($"{(i + 1).Ordinalize()} - <@{user.DiscordId}>")
                    .AppendLine($"Level `{user.CurrentLevel}`")
                    .AppendLine($"XP `{$"{user.TotalXp:n0}"}`")
                    .AppendLine($"Message Count `{user.MessageCount}`")
                    .AppendLine($"Message Efficiency {Formatter.InlineCode(user.GetMessageEfficiency().ToString("P2"))}")
                    .AppendLine($"Average Message Length `{user.GetAverageMessageLength()}`")
                    .AppendLine($"Voice Time `{user.TimeSpentInVoice.Humanize(3)}`")
                    //.AppendLine($"Streaming Time `{user.TimeSpentStreaming.Humanize(3)}`")
                    .AppendLine($"Muted Time `{user.TimeSpentMuted.Humanize(3)}`")
                    .AppendLine($"Deafened Time `{user.TimeSpentDeafened.Humanize(3)}`");

                if (i != orderedByXp.Count - 1)
                {
                    leaderboard.AppendLine();
                }
            }

            var interactivity = context.Client.GetInteractivity();
            var leaderboardPages = interactivity.GeneratePagesInEmbed(leaderboard.ToString(), SplitType.Line, embedBuilder);

            await interactivity.SendPaginatedMessageAsync(context.Channel, context.User, leaderboardPages);
        }
    }
}