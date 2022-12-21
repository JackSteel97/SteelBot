using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DiscordModules.Pets;
using SteelBot.DiscordModules.RankRoles.Helpers;
using SteelBot.Helpers.Levelling;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteelBot.Channels.Voice;

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

    public async Task HandleVoiceStateChange(VoiceStateChange changeArgs)
    {
        (var usersInChannel, var usersInOldChannel) = GetUsersInChannel(changeArgs);

        (double baseScalingFactor, bool shouldEarnVideo) = GetVoiceXpScalingFactors(changeArgs.Guild.Id, changeArgs.User.Id, usersInChannel);

        // Update this user
        await UpdateUser(changeArgs.Guild, changeArgs.User, changeArgs.After, baseScalingFactor, shouldEarnVideo);

        await UpdateOtherUsersStats(changeArgs, usersInChannel);
        // If this user is changing channels to a new channel we need to update the stats of the users in the previous channel too if there are any.
        await UpdateOtherUsersStats(changeArgs, usersInOldChannel);
    }

    private async Task UpdateOtherUsersStats(VoiceStateChange changeArgs, IReadOnlyList<DiscordMember> usersInChannel)
    {
        foreach (var userInChannel in usersInChannel)
        {
            if (userInChannel.Id != changeArgs.User.Id && !userInChannel.IsBot)
            {
                (double otherBaseScalingFactor, bool otherShouldEarnVideo) = GetVoiceXpScalingFactors(changeArgs.Guild.Id, userInChannel.Id, usersInChannel);

                await UpdateUser(changeArgs.Guild, userInChannel, userInChannel.VoiceState, otherBaseScalingFactor, otherShouldEarnVideo);
            }
        }
    }

    private async ValueTask UpdateUser(DiscordGuild guild, DiscordUser user, DiscordVoiceState voiceState, double scalingFactor, bool shouldEarnVideoXp)
    {
        if (await UpdateUserVoiceStats(guild, user, voiceState, scalingFactor, shouldEarnVideoXp))
        {
            await RankRoleShared.UserLevelledUp(guild.Id, user.Id, guild, _rankRolesProvider, _usersCache, _levelMessageSender);
        }
    }

    private static (IReadOnlyList<DiscordMember> usersInNewChannel, IReadOnlyList<DiscordMember> usersInOldChannel) GetUsersInChannel(VoiceStateChange changeArgs)
    {
        IReadOnlyList<DiscordMember> usersInChannel;
        if (changeArgs.After != null && changeArgs.Before != null && changeArgs.After.Channel != null && changeArgs.Before.Channel != null)
        {
            // Changing channel
            var usersInNewChannel = GetUsersInNewChannel(changeArgs);
            var usersInOldChannel = GetUsersInOldChannel(changeArgs);
            return (usersInNewChannel, usersInOldChannel);
        }
        
        if (changeArgs.After != null && changeArgs.After.Channel != null)
        {
            // Joining voice channel.
            usersInChannel = GetUsersInNewChannel(changeArgs);
        }
        else if (changeArgs.Before != null && changeArgs.Before.Channel != null)
        {
            // Leaving voice channel.
            usersInChannel = GetUsersInOldChannel(changeArgs);
        }
        else
        {
            usersInChannel = new List<DiscordMember>();
        }

        return (usersInChannel, new List<DiscordMember>());
    }
    
    private static IReadOnlyList<DiscordMember> GetUsersInNewChannel(VoiceStateChange changeArgs)
    {
        return changeArgs.After.Channel.Users;
    }

    private static IReadOnlyList<DiscordMember> GetUsersInOldChannel(VoiceStateChange changeArgs)
    {
        var usersInChannelJustBeforeLeaving = new List<DiscordMember>();
        usersInChannelJustBeforeLeaving.AddRange(changeArgs.Before.Channel.Users);
        if (changeArgs.Guild.Members.TryGetValue(changeArgs.User.Id, out var thisMember))
        {
            usersInChannelJustBeforeLeaving.Add(thisMember);
        }

        return usersInChannelJustBeforeLeaving;
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

                if (userInChannel?.VoiceState?.IsSelfVideo == true)
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
            if (user.ConsecutiveDaysActive != copyOfUser.ConsecutiveDaysActive)
            {
                // Streak changed.
                ulong xpEarned = copyOfUser.UpdateStreakXp();
                if (xpEarned > 0)
                {
                    _levelMessageSender.SendStreakMessage(guild, discordUser, copyOfUser.ConsecutiveDaysActive, xpEarned);
                }
            }

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