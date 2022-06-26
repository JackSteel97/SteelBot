using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DiscordModules.Pets;
using SteelBot.DiscordModules.RankRoles.Helpers;
using SteelBot.Helpers.Levelling;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteelBot.Channels.Voice
{
    public class VoiceStateChangeHandler
    {
        private readonly ILogger<VoiceStateChangeHandler> _logger;
        private readonly UsersProvider _usersCache;
        private readonly PetsDataHelper _petsDataHelper;
        private readonly LevelMessageSender _levelMessageSender;
        private readonly RankRolesProvider _rankRolesProvider;

        public VoiceStateChangeHandler(ILogger<VoiceStateChangeHandler> logger,
            UsersProvider usersCache,
            PetsDataHelper petsDataHelper,
            LevelMessageSender levelMessageSender,
            RankRolesProvider rankRolesProvider)
        {
            _logger = logger;
            _usersCache = usersCache;
            _petsDataHelper = petsDataHelper;
            _levelMessageSender = levelMessageSender;
            _rankRolesProvider = rankRolesProvider;
        }

        public async Task HandleVoiceStateChange(VoiceStateChange changeArgs, ISpan transaction)
        {
            var getUsersInChannelSpan = transaction.StartChild("Get Users In Channel");
            var usersInChannel = GetUsersInChannel(changeArgs);
            getUsersInChannelSpan.Finish();

            var scalingSpan = transaction.StartChild("Get Xp Scaling Factors");
            (double baseScalingFactor, bool shouldEarnVideo) = GetVoiceXpScalingFactors(changeArgs.Guild.Id, changeArgs.User.Id, usersInChannel);
            scalingSpan.Finish();

            var updateUserSpan = transaction.StartChild("Update User Stats");
            // Update this user
            await UpdateUser(changeArgs.Guild, changeArgs.User, changeArgs.After, baseScalingFactor, shouldEarnVideo);
            updateUserSpan.Finish();

            var updateOtherUsersSpan = transaction.StartChild("Update Other Users In Channel");
            // Update other users in the channel.
            foreach (var userInChannel in usersInChannel)
            {
                if (userInChannel.Id != changeArgs.User.Id && !userInChannel.IsBot)
                {
                    var otherScalingSpan = updateOtherUsersSpan.StartChild("Get Xp Scaling Factors", $"For {userInChannel.Username}");
                    (double otherBaseScalingFactor, bool otherShouldEarnVideo) = GetVoiceXpScalingFactors(changeArgs.Guild.Id, userInChannel.Id, usersInChannel);
                    otherScalingSpan.Finish();

                    var otherUpdateSpan = updateOtherUsersSpan.StartChild("Update User Stats", $"For {userInChannel.Username}");
                    await UpdateUser(changeArgs.Guild, userInChannel, userInChannel.VoiceState, otherBaseScalingFactor, otherShouldEarnVideo);
                    otherUpdateSpan.Finish();
                }
            }
            updateOtherUsersSpan.Finish();
        }

        private async ValueTask UpdateUser(DiscordGuild guild, DiscordUser user, DiscordVoiceState voiceState, double scalingFactor, bool shouldEarnVideoXp)
        {
            if (await UpdateUserVoiceStats(guild, user, voiceState, scalingFactor, shouldEarnVideoXp))
            {
                await RankRoleShared.UserLevelledUp(guild.Id, user.Id, guild, _rankRolesProvider, _usersCache, _levelMessageSender);
            }
        }

        private static IReadOnlyList<DiscordMember> GetUsersInChannel(VoiceStateChange changeArgs)
        {
            IReadOnlyList<DiscordMember> usersInChannel;
            if (changeArgs.After != null && changeArgs.After.Channel != null)
            {
                // Joining voice channel.
                usersInChannel = changeArgs.After.Channel.Users;
            }
            else if (changeArgs.Before != null && changeArgs.Before.Channel != null)
            {
                // Leaving voice channel.
                usersInChannel = changeArgs.Before.Channel.Users;
            }
            else
            {
                usersInChannel = new List<DiscordMember>();
            }

            return usersInChannel;
        }

        private (double baseScalingFactor, bool shouldEarnVideoXp) GetVoiceXpScalingFactors(ulong guildId, ulong currentUserId, IReadOnlyList<DiscordMember> usersInChannel)
        {
            _logger.LogDebug("Calculating Voice Xp scaling factor for User {UserId} in Guild {GuildId}", currentUserId, guildId);
            int otherUsersCount = 0;
            double scalingFactor = 0;

            if (!_usersCache.TryGetUser(guildId, currentUserId, out var thisUser))
            {
                _logger.LogWarning("Could not retrieve data for User {UserId} in Guild {GuildId}", currentUserId, guildId);
                return (scalingFactor, false);
            }

            bool shouldEarnVideoXp = false;
            foreach (var userInChannel in usersInChannel)
            {
                if (userInChannel.Id != currentUserId && !userInChannel.IsBot)
                {
                    ++otherUsersCount;
                    if (_usersCache.TryGetUser(guildId, userInChannel.Id, out var otherUser) && otherUser.CurrentLevel > 0)
                    {
                        scalingFactor += Math.Min((double)otherUser.CurrentLevel / thisUser.CurrentLevel, 5);
                    }

                    if (userInChannel.VoiceState.IsSelfVideo)
                    {
                        shouldEarnVideoXp = true;
                    }
                }
            }

            if (otherUsersCount > 0)
            {
                // Take average scaling factor.
                scalingFactor /= otherUsersCount;
                double groupBonus = (otherUsersCount - 1) / 10D;
                scalingFactor += groupBonus;
            }

            _logger.LogInformation("Voice Xp scaling factor for User {UserId} in Guild {GuildId} is {ScalingFactor} and ShouldEarnVideoXp is {ShouldEarnVideoXp}", currentUserId, guildId, scalingFactor, shouldEarnVideoXp);

            return (scalingFactor, shouldEarnVideoXp);
        }

        private async ValueTask<bool> UpdateUserVoiceStats(DiscordGuild guild, DiscordUser discordUser, DiscordVoiceState newState, double scalingFactor, bool shouldEarnVideoXp)
        {
            ulong guildId = guild.Id;
            ulong userId = discordUser.Id;
            bool levelIncreased = false;

            if (_usersCache.TryGetUser(guildId, userId, out var user))
            {
                _logger.LogInformation("Updating voice state for User {UserId} in Guild {GuildId}", userId, guildId);

                var copyOfUser = user.Clone();
                var availablePets = _petsDataHelper.GetAvailablePets(guildId, userId, out _);
                copyOfUser.VoiceStateChange(newState, availablePets, scalingFactor, shouldEarnVideoXp);

                if (scalingFactor != 0)
                {
                    levelIncreased = copyOfUser.UpdateLevel();
                    await _petsDataHelper.PetXpUpdated(availablePets, guild, copyOfUser.CurrentLevel);
                }

                await _usersCache.UpdateUser(guildId, copyOfUser);

                if (levelIncreased)
                {
                    _levelMessageSender.SendLevelUpMessage(guild, discordUser);
                }
            }

            return levelIncreased;
        }
    }
}
