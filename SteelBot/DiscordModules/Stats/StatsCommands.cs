﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
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
        public async Task TheirStats(CommandContext context, DiscordUser discordUser)
        {
            if (!DataHelper.Stats.TryGetUser(context.Guild.Id, discordUser.Id, out User user))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("I could not find their stats, are they new here?"));
                return;
            }

            DiscordEmbedBuilder embedBuilder = DataHelper.Stats.GetStatsEmbed(user, discordUser.Username);

            await context.RespondAsync(embed: embedBuilder.Build());
        }

        [Command("sendpaginated")]
        public async Task SendPaginated(CommandContext ctx)
        {
            // first retrieve the interactivity module from the client
            var interactivity = ctx.Client.GetInteractivity();

            // generate pages.
            var lipsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Mauris vitae velit eget nunc iaculis laoreet vitae eu risus. Nullam sit amet cursus purus. Duis enim elit, malesuada consequat aliquam sit amet, interdum vel orci. Donec vehicula ut lacus consequat cursus. Aliquam pellentesque eleifend lectus vitae sollicitudin. Vestibulum sit amet risus rhoncus, hendrerit felis eget, tincidunt odio. Nulla sed urna ante. Mauris consectetur accumsan purus, ac dignissim ligula condimentum eu. Phasellus ullamcorper, arcu sed scelerisque tristique, ante elit tincidunt sapien, eu laoreet ipsum mauris eu justo. Curabitur mattis cursus urna, eu ornare lacus pulvinar in. Vivamus cursus gravida nunc. Sed dolor nisi, congue non hendrerit at, rutrum sed mi. Duis est metus, consectetur sed libero quis, dignissim gravida lacus. Mauris suscipit diam dolor, semper placerat justo sodales vel. Curabitur sed fringilla odio.\n\nMorbi pretium placerat nulla sit amet condimentum. Duis placerat, felis ornare vehicula auctor, augue odio consectetur eros, sit amet tristique dolor risus nec leo. Aenean vulputate ipsum sagittis augue malesuada, id viverra odio gravida. Curabitur aliquet elementum feugiat. Phasellus eu faucibus nibh, eget finibus nibh. Proin ac fermentum enim, non consequat orci. Nam quis elit vulputate, mollis eros ut, maximus lacus. Vivamus et lobortis odio. Suspendisse potenti. Fusce nec magna in eros tempor tincidunt non vel mi. Pellentesque auctor eros tellus, vel ultrices mi ultricies eu. Nam pharetra sed tortor id elementum. Donec sit amet mi eleifend, iaculis purus sit amet, interdum turpis.\n\nAliquam at consectetur lectus. Ut et ultrices augue. Etiam feugiat, tortor nec dictum pharetra, nulla mauris convallis magna, quis auctor libero ipsum vitae mi. Mauris posuere feugiat feugiat. Phasellus molestie purus sit amet ipsum sodales, eget pretium lorem pharetra. Quisque in porttitor quam, nec hendrerit ligula. Fusce tempus, diam ut malesuada semper, leo tortor vulputate erat, non porttitor nisi elit eget turpis. Nam vitae arcu felis. Aliquam molestie neque orci, vel consectetur velit mattis vel. Fusce eget tempus leo. Morbi sit amet bibendum mauris. Aliquam erat volutpat. Phasellus nunc lectus, vulputate vitae turpis vel, tristique vulputate nulla. Aenean sit amet augue at mauris laoreet convallis. Nam quis finibus dui, at lobortis lectus.\n\nSuspendisse potenti. Pellentesque massa enim, dapibus at tortor eu, posuere ultricies augue. Nunc condimentum enim id ex sagittis, ut dignissim neque tempor. Nulla cursus interdum turpis. Aenean auctor tempor justo, sed rhoncus lorem sollicitudin quis. Fusce non quam a ante suscipit laoreet eget at ligula. Aenean condimentum consectetur nunc, sit amet facilisis eros lacinia sit amet. Integer quis urna finibus, tristique justo ut, pretium lectus. Proin consectetur enim sed risus rutrum, eu vehicula augue pretium. Vivamus ultricies justo enim, id imperdiet lectus molestie at. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas.\n\nNullam tincidunt dictum nibh, dignissim laoreet libero eleifend ut. Vestibulum eget maximus nulla. Suspendisse a auctor elit, ac facilisis tellus. Sed iaculis turpis ac purus tempor, ut pretium ante ultrices. Aenean commodo tempus vestibulum. Morbi vulputate pharetra molestie. Ut rhoncus quam felis, id mollis quam dapibus id. Curabitur faucibus id justo in ornare. Praesent facilisis dolor lorem, non vulputate velit finibus ut. Praesent vestibulum nunc ac nibh iaculis porttitor.\n\nFusce mattis leo sed ligula laoreet accumsan. Pellentesque tortor magna, ornare vitae tellus eget, mollis placerat est. Suspendisse potenti. Ut sit amet lacus sed nibh pulvinar mattis in bibendum dui. Mauris vitae turpis tempor, malesuada velit in, sodales lacus. Sed vehicula eros in magna condimentum vestibulum. Aenean semper finibus lectus, vel hendrerit lorem euismod a. Sed tempor ante quis magna sollicitudin, eu bibendum risus congue. Donec lectus sem, accumsan ut mollis et, accumsan sed lacus. Nam non dui non tellus pretium mattis. Mauris ultrices et felis ut imperdiet. Nam erat risus, consequat eu eros ac, convallis viverra sapien. Etiam maximus nunc et felis ultrices aliquam.\n\nUt tincidunt at magna at interdum. Sed fringilla in sem non lobortis. In dictum magna justo, nec lacinia eros porta at. Maecenas laoreet mattis vulputate. Sed efficitur tempor euismod. Integer volutpat a odio eu sagittis. Aliquam congue tristique nisi, quis aliquet nunc tristique vitae. Vivamus ac iaculis nunc, et faucibus diam. Donec vitae auctor ipsum, quis posuere est. Proin finibus, dolor ac euismod consequat, urna sem ultrices lectus, in iaculis sem nulla et odio. Integer et vulputate metus. Phasellus finibus et lorem eget lacinia. Maecenas velit est, luctus quis fermentum nec, fringilla eu lorem.\n\nPellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Mauris faucibus neque eu consectetur egestas. Mauris aliquet nibh pellentesque mollis facilisis. Duis egestas lectus sed justo sagittis ultrices. Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Curabitur hendrerit quis arcu id dictum. Praesent in massa eget lectus pulvinar consectetur. Aliquam eget ipsum et velit congue porta vitae ut eros. Quisque convallis lacus et venenatis sagittis. Phasellus sit amet eros ac nibh facilisis laoreet vel eget nisi. In ante libero, volutpat in risus vel, tristique blandit leo. Morbi posuere bibendum libero, non efficitur mi sagittis vel. Cras viverra pulvinar pellentesque. Mauris auctor et lacus ut pellentesque. Nunc pretium luctus nisi eu convallis.\n\nSed nec ultricies arcu. Aliquam eu tincidunt diam, nec luctus ligula. Ut laoreet dignissim est, eu fermentum massa fermentum eget. Nullam non viverra justo, sed congue felis. Phasellus id convallis mauris. Aliquam elementum euismod ex, vitae dignissim nunc consectetur vitae. Donec ut odio quis ex placerat elementum sit amet eget lectus. Suspendisse potenti. Nam non massa id mi suscipit euismod. Nullam varius tincidunt diam congue congue. Proin pharetra vestibulum eros, vel imperdiet sem rutrum at. Cras eget gravida ligula, quis facilisis ex.\n\nEtiam consectetur elit mauris, euismod porta urna auctor a. Nulla facilisi. Praesent massa ipsum, iaculis non odio at, varius lobortis nisi. Aliquam viverra erat a dapibus porta. Pellentesque imperdiet maximus mattis. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Donec luctus elit sit amet feugiat convallis. Phasellus varius, sem ut volutpat vestibulum, magna arcu porttitor libero, in dapibus metus dolor nec dolor. Fusce at eleifend magna. Mauris cursus pellentesque sagittis. Nullam nec laoreet ante, in sodales arcu.";
            var lipsum_pages = interactivity.GeneratePagesInEmbed(lipsum);

            // send the paginator
            await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, lipsum_pages, timeoutoverride: TimeSpan.FromMinutes(5));
        }

        [Command("me")]
        [Description("Displays your user statistics for this server.")]
        public async Task MyStats(CommandContext context)
        {
            if (!DataHelper.Stats.TryGetUser(context.Guild.Id, context.Member.Id, out User user))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("I could not find your stats, are you new here?"));
                return;
            }

            DiscordEmbedBuilder embedBuilder = DataHelper.Stats.GetStatsEmbed(user, context.Member.Username);

            await context.RespondAsync(embed: embedBuilder.Build());
        }

        // TODO: Paginate command response.
        [Command("Leaderboard")]
        [Description("Displays a leaderboard of levels for this server.")]
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
                .WithTitle($"{context.Guild.Name} Leaderboard");

            for (int i = 0; i < orderedByXp.Length; i++)
            {
                User user = orderedByXp[i];
                embedBuilder.AddField($"{(i + 1).Ordinalize()}", $"<@{user.DiscordId}>\nLevel `{user.CurrentLevel}`\nXP `{$"{user.TotalXp:n0}"}`");
            }

            embedBuilder.WithTimestamp(DateTime.UtcNow);

            await context.RespondAsync(embed: embedBuilder.Build());
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

            User[] orderedByXp = guildUsers.OrderByDescending(u => u.TotalXp).Take(top).ToArray();

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
               .WithColor(EmbedGenerator.InfoColour)
               .WithTitle($"{context.Guild.Name} Leaderboard")
               .WithTimestamp(DateTime.UtcNow);

            for (int i = 0; i < orderedByXp.Length; i++)
            {
                User user = orderedByXp[i];
                StringBuilder detail = new StringBuilder();
                detail.AppendLine($"<@{user.DiscordId}>")
                    .AppendLine($"Level `{user.CurrentLevel}`")
                    .AppendLine($"XP `{$"{user.TotalXp:n0}"}`")
                    .AppendLine($"Message Count `{user.MessageCount}`")
                    .AppendLine($"Average Message Length `{user.GetAverageMessageLength()}`")
                    .AppendLine($"Voice Time `{user.TimeSpentInVoice.Humanize(3)}`")
                    //.AppendLine($"Streaming Time `{user.TimeSpentStreaming.Humanize(3)}`")
                    .AppendLine($"Muted Time `{user.TimeSpentMuted.Humanize(3)}`")
                    .AppendLine($"Deafened Time `{user.TimeSpentDeafened.Humanize(3)}`")
                    .AppendLine($"Deafened Time `{user.TimeSpentDeafened.Humanize(3)}`");

                embedBuilder.AddField($"{(i + 1).Ordinalize()}", detail.ToString());
            }

            await context.RespondAsync(embed: embedBuilder.Build());
        }
    }
}