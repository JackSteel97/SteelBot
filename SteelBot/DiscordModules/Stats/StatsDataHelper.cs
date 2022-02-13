using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Humanizer;
using Microsoft.Extensions.Logging;
using SteelBot.Database.Models;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets;
using SteelBot.Helpers;
using SteelBot.Helpers.Levelling;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Stats
{
    public class StatsDataHelper
    {
        private readonly DataCache Cache;
        private readonly AppConfigurationService AppConfigurationService;
        private readonly ILogger<StatsDataHelper> Logger;
        private readonly PetsDataHelper PetsDataHelper;

        public StatsDataHelper(DataCache cache, AppConfigurationService appConfigurationService, ILogger<StatsDataHelper> logger, PetsDataHelper petsDataHelper)
        {
            Cache = cache;
            AppConfigurationService = appConfigurationService;
            Logger = logger;
            PetsDataHelper = petsDataHelper;
        }

        public async Task<bool> HandleNewMessage(MessageCreateEventArgs args)
        {
            bool levelIncreased = false;
            if(TryGetUser(args.Guild.Id, args.Author.Id, out User user))
            {
                Logger.LogInformation("Updating message counters for User [{UserId}] in Guild [{GuildId}]", args.Author.Id, args.Guild.Id);
                // Clone user to avoid making change to cache till db change confirmed.
                User copyOfUser = user.Clone();
                var availablePets = PetsDataHelper.GetAvailablePets(args.Guild.Id, args.Author.Id, out _);
                if (copyOfUser.NewMessage(args.Message.Content.Length, availablePets))
                {
                    // Xp has changed.
                    levelIncreased = copyOfUser.UpdateLevel();
                    await PetsDataHelper.PetXpsUpdated(availablePets);
                }
                await Cache.Users.UpdateUser(args.Guild.Id, copyOfUser);

                if (levelIncreased)
                {
                    await SendLevelUpMessage(args.Guild, args.Author);
                }
            }
            
            return levelIncreased;
        }

        public DiscordEmbedBuilder GetStatsEmbed(User user, string username)
        {
            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
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

        public async Task<bool> HandleVoiceStateChange(VoiceStateUpdateEventArgs args)
        {
            ulong guildId = args.Guild.Id;
            ulong userId = args.User.Id;
            bool levelIncreased = false;

            if(TryGetUser(guildId, userId, out User user))
            {
                Logger.LogInformation("Updating voice state for User [{UserId}] in Guild [{GuildId}]", userId, guildId);

                User copyOfUser = user.Clone();
                var availablePets = PetsDataHelper.GetAvailablePets(guildId, userId, out _);
                copyOfUser.VoiceStateChange(args.After, availablePets);

                levelIncreased = copyOfUser.UpdateLevel();
                await Cache.Users.UpdateUser(guildId, copyOfUser);
                await PetsDataHelper.PetXpsUpdated(availablePets);

                if (levelIncreased)
                {
                    await SendLevelUpMessage(args.Guild, args.User);
                }
            }

            return levelIncreased;
        }

        /// <summary>
        /// Called during app shutdown to make sure no timings get carried too long during downtime.
        /// </summary>
        public async Task DisconnectAllUsers()
        {
            Logger.LogInformation("Disconnecting all users from voice stats");
            var allUsers = Cache.Users.GetAllUsers();

            foreach (var user in allUsers)
            {
                User copyOfUser = user.Clone();
                var availablePets = PetsDataHelper.GetAvailablePets(user.Guild.DiscordId, user.DiscordId, out _);

                // Pass null to reset all start times.
                copyOfUser.VoiceStateChange(newState: null, availablePets);
                copyOfUser.UpdateLevel();
                await Cache.Users.UpdateUser(user.Guild.DiscordId, copyOfUser);
                await PetsDataHelper.PetXpsUpdated(availablePets);
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

        private async Task SendLevelUpMessage(DiscordGuild discordGuild, DiscordUser discordUser)
        {
            if (Cache.Guilds.TryGetGuild(discordGuild.Id, out Guild guild) && Cache.Users.TryGetUser(discordGuild.Id, discordUser.Id, out User user))
            {
                DiscordChannel channel = guild.GetLevelAnnouncementChannel(discordGuild);

                if (channel != null)
                {
                    await channel.SendMessageAsync(embed: EmbedGenerator.Info($"{discordUser.Mention} just advanced to level {user.CurrentLevel}!", "LEVEL UP!", $"Use {guild.CommandPrefix}Stats Me to check your progress"));
                }
            }
        }
    }
}