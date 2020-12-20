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
        private readonly DataHelpers DataHelper;

        public StatsCommands(DataHelpers dataHelpers)
        {
            DataHelper = dataHelpers;
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