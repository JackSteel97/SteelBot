using DSharpPlus.Entities;
using SteelBot.Database.Models.Pets;
using SteelBot.Database.Models.Users;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;

namespace SteelBot.Helpers.Levelling;

public static class UserExtensions
{
    private static readonly TimeSpan _maxDisconnectedDuration = TimeSpan.FromHours(8);

    public static LevellingConfig LevelConfig { get; set; }

    public static bool UpdateLevel(this User user)
    {
        bool levelIncreased = LevellingMaths.UpdateLevel(user.CurrentLevel, user.TotalXp, out int newLevel);

        if (levelIncreased) user.CurrentLevel = newLevel;
        return levelIncreased;
    }

    public static ulong UpdateStreakXp(this User user)
    {
        ulong xpEarned = Convert.ToUInt64(user.ConsecutiveDaysActive switch
        {
            1 => 50,
            2 => 100,
            3 => 200,
            4 => 400,
            5 => 600,
            6 => 800,
            7 => 1000,
            10 => 2000,
            15 => 3000,
            20 => 4000,
            25 => 5000,
            30 => 10_000,
            45 => 20_000,
            60 => 50_000,
            90 => 100_000,
            > 100 when user.ConsecutiveDaysActive % 10 == 0 => user.ConsecutiveDaysActive * 1000,
            _ => 0
        });

        user.ActivityStreakXpEarned += xpEarned;
        return xpEarned;
    }

    public static bool NewMessage(this User user, int messageLength, List<Pet> availablePets)
    {
        var messageReceivedAt = DateTime.UtcNow;
        ++user.MessageCount;
        user.TotalMessageLength += Convert.ToUInt64(messageLength);
        UpdateActivityStreak(user, messageReceivedAt);

        bool lastMessageWasMoreThanAMinuteAgo = (messageReceivedAt - user.LastXpEarningMessage.GetValueOrDefault()).TotalSeconds >= 60;
        if (lastMessageWasMoreThanAMinuteAgo)
        {
            user.LastXpEarningMessage = messageReceivedAt;
            user.MessageXpEarned += LevellingMaths.ApplyPetBonuses(LevelConfig.MessageXp, availablePets, BonusType.MessageXp);
        }

        user.LastActivity = messageReceivedAt;
        user.LastMessageSent = messageReceivedAt;

        return lastMessageWasMoreThanAMinuteAgo;
    }

    public static void VoiceStateChange(this User user, DiscordVoiceState newState, List<Pet> availablePets, double scalingFactor, bool shouldEarnVideoXp, bool updateLastActivity = true)
    {
        var now = DateTime.UtcNow;
        if (updateLastActivity)
        {
            user.LastActivity = now;
            UpdateActivityStreak(user, now);
        }

        UpdateVoiceCounters(user, now, availablePets, scalingFactor, shouldEarnVideoXp);
        UpdateStartTimes(user, newState, now, scalingFactor);
    }

    private static void UpdateActivityStreak(User user, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        if (user.LastActiveDay == default)
        {
            user.LastActiveDay = today;
            user.ConsecutiveDaysActive = 1;
        }
        else
        {
            int daysSinceLastActive = today.DayNumber - user.LastActiveDay.DayNumber;
            switch (daysSinceLastActive)
            {
                case > 1:
                    // Broken streak
                    user.LastActiveDay = today;
                    user.ConsecutiveDaysActive = 1;
                    break;
                case 1:
                    // Continuing streak
                    user.LastActiveDay = today;
                    user.ConsecutiveDaysActive++;
                    break;
            }
        }
    }

    private static void UpdateStartTimes(User user, DiscordVoiceState newState, DateTime now, double scalingFactor)
    {
        if (newState == null || newState.Channel == null)
        {
            // User has left voice channel - reset all states.
            user.VoiceStartTime = null;
            user.MutedStartTime = null;
            user.DeafenedStartTime = null;
            user.StreamingStartTime = null;
            user.VideoStartTime = null;
            user.AfkStartTime = null;
            user.DisconnectedStartTime = now;
        }
        else if (scalingFactor == 0 || newState.Channel == newState.Guild.AfkChannel)
        {
            // User has gone AFK or is alone, this doesn't count as being in a normal voice channel.
            user.AfkStartTime = newState.Channel == newState.Guild.AfkChannel ? now : null;
            user.VoiceStartTime = null;
            user.MutedStartTime = null;
            user.DeafenedStartTime = null;
            user.StreamingStartTime = null;
            user.VideoStartTime = null;
            user.DisconnectedStartTime = null;
        }
        else
        {
            // User is in a non-AFK voice channel.
            user.VoiceStartTime = now;
            user.MutedStartTime = newState.IsSelfMuted ? now : null;
            user.DeafenedStartTime = newState.IsSelfDeafened ? now : null;
            user.StreamingStartTime = newState.IsSelfStream ? now : null;
            user.VideoStartTime = newState.IsSelfVideo ? now : null;
            user.AfkStartTime = null;
            user.DisconnectedStartTime = null;
        }
    }

    private static void UpdateVoiceCounters(User user, DateTime now, List<Pet> availablePets, double scalingFactor, bool shouldEarnVideoXp)
    {
        if (user.VoiceStartTime.HasValue)
        {
            var durationDifference = now - user.VoiceStartTime.Value;
            user.TimeSpentInVoice += durationDifference;
            IncrementVoiceXp(user, durationDifference, availablePets, scalingFactor);
        }

        if (user.MutedStartTime.HasValue)
        {
            var durationDifference = now - user.MutedStartTime.Value;
            user.TimeSpentMuted += durationDifference;
            IncrementMutedXp(user, durationDifference, availablePets, scalingFactor);
        }

        if (user.DeafenedStartTime.HasValue)
        {
            var durationDifference = now - user.DeafenedStartTime.Value;
            user.TimeSpentDeafened += durationDifference;
            IncrementDeafenedXp(user, durationDifference, availablePets, scalingFactor);
        }

        if (user.StreamingStartTime.HasValue)
        {
            var durationDifference = now - user.StreamingStartTime.Value;
            user.TimeSpentStreaming += durationDifference;
            IncrementStreamingXp(user, durationDifference, availablePets, scalingFactor);
        }

        if (user.VideoStartTime.HasValue)
        {
            var durationDifference = now - user.VideoStartTime.Value;
            user.TimeSpentOnVideo += durationDifference;
            IncrementVideoXp(user, durationDifference, availablePets, scalingFactor, shouldEarnVideoXp);
        }

        if (user.AfkStartTime.HasValue) user.TimeSpentAfk += now - user.AfkStartTime.Value;
        // No XP earned or lost for AFK.
        if (user.DisconnectedStartTime.HasValue)
        {
            var durationDifference = now - user.DisconnectedStartTime.Value;
            user.TimeSpentDisconnected += durationDifference;
            IncrementDisconnectedXp(user, durationDifference, availablePets);
        }
    }

    private static void IncrementVoiceXp(User user, TimeSpan voiceDuration, List<Pet> availablePets, double scalingFactor)
    {
        if (scalingFactor != 0)
        {
            ulong baseXp = LevellingMaths.GetDurationXp(voiceDuration, user.TimeSpentInVoice, LevelConfig.VoiceXpPerMin);
            ulong bonusAppliedXp = LevellingMaths.ApplyPetBonuses(baseXp, availablePets, BonusType.VoiceXp);
            user.VoiceXpEarned += LevellingMaths.ApplyScalingFactor(bonusAppliedXp, scalingFactor);
        }
    }

    private static void IncrementMutedXp(User user, TimeSpan mutedDuration, List<Pet> availablePets, double scalingFactor)
    {
        if (scalingFactor != 0)
        {
            ulong baseXp = LevellingMaths.GetDurationXp(mutedDuration, user.TimeSpentMuted, LevelConfig.MutedXpPerMin);
            ulong bonusAppliedXp = LevellingMaths.ApplyPetBonuses(baseXp, availablePets, BonusType.MutedPenaltyXp);
            user.MutedXpEarned += LevellingMaths.ApplyScalingFactor(bonusAppliedXp, scalingFactor);
        }
    }

    private static void IncrementDeafenedXp(User user, TimeSpan deafenedXp, List<Pet> availablePets, double scalingFactor)
    {
        if (scalingFactor != 0)
        {
            ulong baseXp = LevellingMaths.GetDurationXp(deafenedXp, user.TimeSpentDeafened, LevelConfig.DeafenedXpPerMin);
            ulong bonusAppliedXp = LevellingMaths.ApplyPetBonuses(baseXp, availablePets, BonusType.DeafenedPenaltyXp);
            user.DeafenedXpEarned += LevellingMaths.ApplyScalingFactor(bonusAppliedXp, scalingFactor);
        }
    }

    private static void IncrementStreamingXp(User user, TimeSpan streamingDuration, List<Pet> availablePets, double scalingFactor)
    {
        if (scalingFactor != 0)
        {
            ulong baseXp = LevellingMaths.GetDurationXp(streamingDuration, user.TimeSpentStreaming, LevelConfig.StreamingXpPerMin);
            ulong bonusAppliedXp = LevellingMaths.ApplyPetBonuses(baseXp, availablePets, BonusType.StreamingXp);
            user.StreamingXpEarned += LevellingMaths.ApplyScalingFactor(bonusAppliedXp, scalingFactor);
        }
    }

    private static void IncrementVideoXp(User user, TimeSpan videoDuration, List<Pet> availablePets, double scalingFactor, bool shouldEarnVideoXp)
    {
        if (scalingFactor != 0 && shouldEarnVideoXp)
        {
            ulong baseXp = LevellingMaths.GetDurationXp(videoDuration, user.TimeSpentOnVideo, LevelConfig.VideoXpPerMin);
            ulong bonusAppliedXp = LevellingMaths.ApplyPetBonuses(baseXp, availablePets, BonusType.VideoXp);
            user.VideoXpEarned += LevellingMaths.ApplyScalingFactor(bonusAppliedXp, scalingFactor);
        }
    }

    private static void IncrementDisconnectedXp(User user, TimeSpan disconnectedDuration, List<Pet> availablePets)
    {
        double disconnectedXpPerMin = PetShared.GetBonusValue(availablePets, BonusType.OfflineXp);
        if (disconnectedXpPerMin > 0)
        {
            var duration = disconnectedDuration;
            if (disconnectedDuration > _maxDisconnectedDuration) duration = _maxDisconnectedDuration;
            ulong xpEarned = LevellingMaths.GetDurationXp(duration, TimeSpan.Zero, disconnectedXpPerMin);
            user.DisconnectedXpEarned += xpEarned;
        }
    }
}