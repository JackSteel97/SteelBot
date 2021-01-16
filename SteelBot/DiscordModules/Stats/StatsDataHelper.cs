using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Humanizer;
using Microsoft.Extensions.Logging;
using SteelBot.Database.Models;
using SteelBot.DataProviders;
using SteelBot.Helpers;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Stats
{
    public class StatsDataHelper
    {
        private readonly ILogger<StatsDataHelper> Logger;
        private readonly DataCache Cache;

        public StatsDataHelper(ILogger<StatsDataHelper> logger, DataCache cache)
        {
            Logger = logger;
            Cache = cache;
        }

        public async Task<bool> HandleNewMessage(MessageCreateEventArgs args)
        {
            // Update per-user message counters.
            bool levelIncreased = await Cache.Users.UpdateMessageCounters(args.Guild.Id, args.Author.Id, args.Message.Content.Length);
            if (levelIncreased)
            {
                await SendLevelUpMessage(args.Guild, args.Author);
            }
            return levelIncreased;
        }

        public DiscordEmbedBuilder GetStatsEmbed(User user, string username)
        {
            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithColor(EmbedGenerator.InfoColour)
                .WithTitle($"{username} Stats")
                .AddField("Message Count", $"`{user.MessageCount} Messages`", true)
                .AddField("Average Message Length", $"`{user.GetAverageMessageLength()} Characters`", true)
                .AddField("Message Efficiency", Formatter.InlineCode(user.GetMessageEfficiency().ToString("P2")), true)
                .AddField("Voice Time", $"`{user.TimeSpentInVoice.Humanize(3)}`", true)
                .AddField("Muted Time", $"`{user.TimeSpentMuted.Humanize(3)}`", true)
                //.AddField("\u200B", "\u200B") TODO: Uncomment when adding streaming time for 2x2 grid of times.
                .AddField("Deafened Time", $"`{user.TimeSpentDeafened.Humanize(3)}`", false);
            //.AddField("Streaming Time", $"`{user.TimeSpentStreaming.Humanize(3)}`", false)
            return embedBuilder;
        }

        public async Task<bool> HandleVoiceStateChange(VoiceStateUpdateEventArgs args)
        {
            ulong guildId = args.Guild.Id;
            ulong userId = args.User.Id;

            bool levelIncreased = await Cache.Users.UpdateVoiceStateCounters(guildId, userId, args.After);

            if (levelIncreased)
            {
                await SendLevelUpMessage(args.Guild, args.User);
            }

            return levelIncreased;
        }

        private async Task SendLevelUpMessage(DiscordGuild discordGuild, DiscordUser discordUser)
        {
            if (Cache.Guilds.TryGetGuild(discordGuild.Id, out Guild guild) && Cache.Users.TryGetUser(discordGuild.Id, discordUser.Id, out User user))
            {
                if (guild.LevelAnnouncementChannelId.HasValue)
                {
                    DiscordChannel channel = discordGuild.GetChannel(guild.LevelAnnouncementChannelId.Value);
                    await channel.SendMessageAsync(embed: EmbedGenerator.Info($"{discordUser.Mention} just advanced to level {user.CurrentLevel}!", "LEVEL UP!"));
                }
            }
        }

        public bool TryGetUser(ulong guildId, ulong discordId, out User user)
        {
            return Cache.Users.TryGetUser(guildId, discordId, out user);
        }

        public List<User> GetUsersInGuild(ulong guildId)
        {
            return Cache.Users.GetUsersInGuild(guildId);
        }

        public List<CommandStatistic> GetCommandStatistics()
        {
            return Cache.CommandStatistics.GetAllCommandStatistics();
        }
    }
}