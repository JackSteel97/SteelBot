using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Humanizer;
using Microsoft.Extensions.Logging;
using SteelBot.Database.Models;
using SteelBot.Database.Models.Pets;
using SteelBot.Database.Models.Users;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.DiscordModules.RankRoles;
using SteelBot.DiscordModules.Stats.Models;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Helpers.Levelling;
using SteelBot.Services;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Stats
{
    public class StatsDataHelper
    {
        private readonly DataCache Cache;
        private readonly AppConfigurationService AppConfigurationService;
        private readonly ILogger<StatsDataHelper> Logger;
        private readonly PetsDataHelper PetsDataHelper;
        private readonly RankRoleDataHelper RankRoleDataHelper;
        private readonly ErrorHandlingService ErrorHandlingService;

        public StatsDataHelper(DataCache cache, AppConfigurationService appConfigurationService, ILogger<StatsDataHelper> logger, PetsDataHelper petsDataHelper, RankRoleDataHelper rankRoleDataHelper, ErrorHandlingService errorHandlingService)
        {
            Cache = cache;
            AppConfigurationService = appConfigurationService;
            Logger = logger;
            PetsDataHelper = petsDataHelper;
            RankRoleDataHelper = rankRoleDataHelper;
            ErrorHandlingService = errorHandlingService;
        }

        public async Task<bool> HandleNewMessage(MessageCreateEventArgs args)
        {
            bool levelIncreased = false;
            if (TryGetUser(args.Guild.Id, args.Author.Id, out User user))
            {
                Logger.LogInformation("Updating message counters for User [{UserId}] in Guild [{GuildId}]", args.Author.Id, args.Guild.Id);
                // Clone user to avoid making change to cache till db change confirmed.
                User copyOfUser = user.Clone();
                var availablePets = PetsDataHelper.GetAvailablePets(args.Guild.Id, args.Author.Id, out _);
                if (copyOfUser.NewMessage(args.Message.Content.Length, availablePets))
                {
                    // Xp has changed.
                    levelIncreased = copyOfUser.UpdateLevel();
                    await PetsDataHelper.PetXpUpdated(availablePets, args.Guild);
                }
                await Cache.Users.UpdateUser(args.Guild.Id, copyOfUser);

                if (levelIncreased)
                {
                    SendLevelUpMessage(args.Guild, args.Author);
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

        public async Task HandleVoiceStateChange(VoiceStateUpdateEventArgs args)
        {
            IReadOnlyList<DiscordMember> usersInChannel;
            if (args.After != null && args.After.Channel != null)
            {
                // Joining voice channel.
                usersInChannel = args.After.Channel.Users;
            }
            else if (args.Before != null && args.Before.Channel != null)
            {
                // Leaving voice channel.
                usersInChannel = args.Before.Channel.Users;
            }
            else
            {
                usersInChannel = new List<DiscordMember>();
            }

            var scalingFactor = GetVoiceXpScalingFactor(args.Guild.Id, args.User.Id, usersInChannel);

            // Update this user
            if (await UpdateUserVoiceStats(args.Guild, args.User, args.After, scalingFactor))
            {
                await RankRoleDataHelper.UserLevelledUp(args.Guild.Id, args.User.Id, args.Guild);
            }

            foreach (var userInChannel in usersInChannel)
            {
                if (userInChannel.Id != args.User.Id)
                {
                    var otherScalingFactor = GetVoiceXpScalingFactor(args.Guild.Id, userInChannel.Id, usersInChannel);
                    // Update other users.
                    if (await UpdateUserVoiceStats(args.Guild, userInChannel, userInChannel.VoiceState, otherScalingFactor))
                    {
                        await RankRoleDataHelper.UserLevelledUp(args.Guild.Id, userInChannel.Id, args.Guild);
                    }
                }
            }
        }

        private async Task<bool> UpdateUserVoiceStats(DiscordGuild guild, DiscordUser discordUser, DiscordVoiceState newState, double scalingFactor)
        {
            ulong guildId = guild.Id;
            ulong userId = discordUser.Id;
            bool levelIncreased = false;

            if (TryGetUser(guildId, userId, out User user))
            {
                Logger.LogInformation("Updating voice state for User [{UserId}] in Guild [{GuildId}]", userId, guildId);

                User copyOfUser = user.Clone();
                var availablePets = PetsDataHelper.GetAvailablePets(guildId, userId, out _);
                copyOfUser.VoiceStateChange(newState, availablePets, scalingFactor);

                if (scalingFactor != 0)
                {
                    levelIncreased = copyOfUser.UpdateLevel();
                    await PetsDataHelper.PetXpUpdated(availablePets, guild);
                }

                await Cache.Users.UpdateUser(guildId, copyOfUser);

                if (levelIncreased)
                {
                    SendLevelUpMessage(guild, discordUser);
                }
            }

            return levelIncreased;
        }

        private double GetVoiceXpScalingFactor(ulong guildId, ulong currentUserId, IReadOnlyList<DiscordMember> usersInChannel)
        {
            Logger.LogInformation("Calculating Voice Xp scaling factor for User {UserId} in Guild {GuildId}", currentUserId, guildId);
            int otherUsersCount = 0;
            double scalingFactor = 0;

            if (!Cache.Users.TryGetUser(guildId, currentUserId, out var thisUser))
            {
                Logger.LogWarning("Could not retrieve data for User {UserId} in Guild {GuildId}", currentUserId, guildId);
                return scalingFactor;
            }

            foreach (var userInChannel in usersInChannel)
            {
                if (userInChannel.Id != currentUserId)
                {
                    ++otherUsersCount;
                    if (Cache.Users.TryGetUser(guildId, userInChannel.Id, out var otherUser) && otherUser.CurrentLevel > 0)
                    {
                        scalingFactor += Math.Min((double)otherUser.CurrentLevel / thisUser.CurrentLevel, 5);
                    }
                }
            }

            if (otherUsersCount > 0)
            {
                // Take average scaling factor.
                scalingFactor /= otherUsersCount;
                var groupBonus = (otherUsersCount - 1) / 10D;
                scalingFactor += groupBonus;
            }

            Logger.LogInformation("Voice Xp scaling factor for User {UserId} in Guild {GuildId} is {ScalingFactor}", currentUserId, guildId, scalingFactor);

            return scalingFactor;
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
                copyOfUser.VoiceStateChange(newState: null, availablePets, scalingFactor: 1, updateLastActivity: false);
                copyOfUser.UpdateLevel();
                await Cache.Users.UpdateUser(user.Guild.DiscordId, copyOfUser);
                await PetsDataHelper.PetXpUpdated(availablePets, default); // Default - Don't try to send level up messages
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

        public XpVelocity GetVelocity(DiscordMember target, List<Pet> availablePets)
        {
            var velocity = new XpVelocity();
            var baseDuration = TimeSpan.FromMinutes(1);
            var levelConfig = AppConfigurationService.Application.Levelling;

            if (Cache.Users.TryGetUser(target.Guild.Id, target.Id, out var user))
            {
                velocity.Message = LevellingMaths.ApplyPetBonuses(levelConfig.MessageXp, availablePets, BonusType.MessageXP);
                velocity.Voice = LevellingMaths.GetDurationXp(baseDuration, user.TimeSpentInVoice, availablePets, BonusType.VoiceXP, levelConfig.VoiceXpPerMin);
                velocity.Muted = LevellingMaths.GetDurationXp(baseDuration, user.TimeSpentMuted, availablePets, BonusType.MutedPenaltyXP, levelConfig.MutedXpPerMin);
                velocity.Deafened = LevellingMaths.GetDurationXp(baseDuration, user.TimeSpentDeafened, availablePets, BonusType.DeafenedPenaltyXP, levelConfig.DeafenedXpPerMin);
                velocity.Streaming = LevellingMaths.GetDurationXp(baseDuration, user.TimeSpentStreaming, availablePets, BonusType.StreamingXP, levelConfig.StreamingXpPerMin);
                velocity.Video = LevellingMaths.GetDurationXp(baseDuration, user.TimeSpentOnVideo, availablePets, BonusType.VideoXP, levelConfig.VideoXpPerMin);

                var disconnectedXpPerMin = PetShared.GetDisconnectedXpPerMin(availablePets);
                velocity.Passive = LevellingMaths.GetDurationXp(baseDuration, TimeSpan.Zero, disconnectedXpPerMin);
            }
            return velocity;
        }

        private void SendLevelUpMessage(DiscordGuild discordGuild, DiscordUser discordUser)
        {
            if (Cache.Guilds.TryGetGuild(discordGuild.Id, out Guild guild) && Cache.Users.TryGetUser(discordGuild.Id, discordUser.Id, out User user))
            {
                DiscordChannel channel = guild.GetLevelAnnouncementChannel(discordGuild);

                if (channel != null)
                {
                    channel.SendMessageAsync(embed: EmbedGenerator.Info($"{discordUser.Mention} just advanced to level {user.CurrentLevel}!", "LEVEL UP!", $"Use {guild.CommandPrefix}Stats Me to check your progress"))
                        .FireAndForget(ErrorHandlingService);
                }
            }
        }
    }
}