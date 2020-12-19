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
                .AddField("Voice Time", $"`{user.TimeSpentInVoice.Humanize(3)}`", false)
                //.AddField("Streaming Time", $"`{user.TimeSpentStreaming.Humanize(3)}`", false)
                .AddField("Muted Time", $"`{user.TimeSpentMuted.Humanize(3)}`", false)
                .AddField("Deafened Time", $"`{user.TimeSpentDeafened.Humanize(3)}`", false);
            return embedBuilder;
        }

        public async Task HandleVoiceStateChange(VoiceStateUpdateEventArgs args)
        {
            ulong guildId = args.Guild.Id;
            ulong userId = args.User.Id;

            var beforeChannel = args.Before?.Channel;
            var afterChannel = args.After?.Channel;

            bool levelIncreased = false;

            // Voice channel state.
            if (beforeChannel == null && afterChannel != null)
            {
                Logger.LogInformation($"User [{userId}] joined voice channel [{afterChannel.Name}]");
                levelIncreased = await Cache.Users.UpdateVoiceState(guildId, userId, true);
            }
            else if (beforeChannel != null && afterChannel == null)
            {
                Logger.LogInformation($"User [{userId}] left voice channel [{beforeChannel.Name}]");
                levelIncreased = await Cache.Users.UpdateVoiceState(guildId, userId, false);
                // Set streaming to false - streaming is still true when leaving a voice channel while streaming.
                levelIncreased = await Cache.Users.UpdateStreamingState(guildId, userId, false) || levelIncreased;
            }
            else if (beforeChannel != null && afterChannel != null && beforeChannel.Id != afterChannel.Id)
            {
                // Direct channel-channel change - we don't care about this state change.
                Logger.LogDebug($"User [{args.User.Id}] changed voice channel from [{beforeChannel.Name}] to [{afterChannel.Name}]");
            }

            // Mute state.
            if (args.Before?.IsSelfMuted != args.After?.IsSelfMuted)
            {
                Logger.LogInformation($"User [{userId}] muted state changed to [{args.After?.IsSelfMuted}]");
                levelIncreased = await Cache.Users.UpdateMutedState(guildId, userId, (args.After?.IsSelfMuted).GetValueOrDefault()) || levelIncreased;
            }

            // Deafened state.
            if (args.Before?.IsSelfDeafened != args.After?.IsSelfDeafened)
            {
                Logger.LogInformation($"User [{userId}] deafened state changed to [{args.After?.IsSelfDeafened}]");
                levelIncreased = await Cache.Users.UpdateDeafendedState(guildId, userId, (args.After?.IsSelfDeafened).GetValueOrDefault()) || levelIncreased;
            }

            // TODO: Add streaming state support.

            if (levelIncreased)
            {
                await SendLevelUpMessage(args.Guild, args.User);
            }
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
    }
}