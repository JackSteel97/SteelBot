using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DiscordModules.Pets;
using SteelBot.DiscordModules.RankRoles;
using SteelBot.Helpers.Levelling;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.Channels.Voice
{
    public class VoiceStateChangeHandler
    {
        private readonly ILogger<VoiceStateChangeHandler> _logger;
        private readonly UsersProvider _usersCache;
        private readonly PetsDataHelper _petsDataHelper;
        private readonly LevelMessageSender _levelMessageSender;
        private readonly RankRoleDataHelper _rankRoleDataHelper;

        public VoiceStateChangeHandler(ILogger<VoiceStateChangeHandler> logger, UsersProvider usersCache, PetsDataHelper petsDataHelper, LevelMessageSender levelMessageSender, RankRoleDataHelper rankRoleDataHelper)
        {
            _logger = logger;
            _usersCache = usersCache;
            _petsDataHelper = petsDataHelper;
            _levelMessageSender = levelMessageSender;
            _rankRoleDataHelper = rankRoleDataHelper;
        }

        public async Task HandleVoiceStateChange(VoiceStateChange changeArgs)
        {
            var usersInChannel = GetUsersInChannel(changeArgs);

            var scalingFactor = GetVoiceXpScalingFactor(changeArgs.Guild.Id, changeArgs.User.Id, usersInChannel);

            // Update this user
            await UpdateUser(changeArgs.Guild, changeArgs.User, changeArgs.After, scalingFactor);

            // Update other users in the channel.
            foreach(var userInChannel in usersInChannel)
            {
                if(userInChannel.Id != changeArgs.User.Id)
                {
                    var otherScalingFactor = GetVoiceXpScalingFactor(changeArgs.Guild.Id, userInChannel.Id, usersInChannel);
                    await UpdateUser(changeArgs.Guild, userInChannel, userInChannel.VoiceState, otherScalingFactor);
                }
            }
        }

        private async Task UpdateUser(DiscordGuild guild, DiscordUser user, DiscordVoiceState voiceState, double scalingFactor)
        {
            if (await UpdateUserVoiceStats(guild, user, voiceState, scalingFactor))
            {
                await _rankRoleDataHelper.UserLevelledUp(guild.Id, user.Id, guild);
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

        private double GetVoiceXpScalingFactor(ulong guildId, ulong currentUserId, IReadOnlyList<DiscordMember> usersInChannel)
        {
            _logger.LogDebug("Calculating Voice Xp scaling factor for User {UserId} in Guild {GuildId}", currentUserId, guildId);
            int otherUsersCount = 0;
            double scalingFactor = 0;

            if (!_usersCache.TryGetUser(guildId, currentUserId, out var thisUser))
            {
                _logger.LogWarning("Could not retrieve data for User {UserId} in Guild {GuildId}", currentUserId, guildId);
                return scalingFactor;
            }

            foreach (var userInChannel in usersInChannel)
            {
                if (userInChannel.Id != currentUserId)
                {
                    ++otherUsersCount;
                    if (_usersCache.TryGetUser(guildId, userInChannel.Id, out var otherUser) && otherUser.CurrentLevel > 0)
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

            _logger.LogInformation("Voice Xp scaling factor for User {UserId} in Guild {GuildId} is {ScalingFactor}", currentUserId, guildId, scalingFactor);

            return scalingFactor;
        }

        private async Task<bool> UpdateUserVoiceStats(DiscordGuild guild, DiscordUser discordUser, DiscordVoiceState newState, double scalingFactor)
        {
            ulong guildId = guild.Id;
            ulong userId = discordUser.Id;
            bool levelIncreased = false;

            if (_usersCache.TryGetUser(guildId, userId, out var user))
            {
                _logger.LogInformation("Updating voice state for User [{UserId}] in Guild [{GuildId}]", userId, guildId);

                var copyOfUser = user.Clone();
                var availablePets = _petsDataHelper.GetAvailablePets(guildId, userId, out _);
                copyOfUser.VoiceStateChange(newState, availablePets, scalingFactor);

                if (scalingFactor != 0)
                {
                    levelIncreased = copyOfUser.UpdateLevel();
                    await _petsDataHelper.PetXpUpdated(availablePets, guild);
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
