﻿using DSharpPlus.Entities;
using SteelBot.Database.Models;
using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.Helpers.Levelling
{
    public static class UserExtensions
    {
        public static LevellingConfig LevelConfig { get; set; }

        public static bool UpdateLevel(this User user)
        {
            int newLevel = user.CurrentLevel;

            bool hasEnoughXp;

            do
            {
                ulong requiredXp = LevellingMaths.XpForLevel(newLevel + 1);
                hasEnoughXp = user.TotalXp >= requiredXp;
                if (hasEnoughXp)
                {
                    ++newLevel;
                }
            } while (hasEnoughXp);

            bool levelIncreased = newLevel > user.CurrentLevel;
            if (levelIncreased)
            {
                user.CurrentLevel = newLevel;
            }
            return levelIncreased;
        }

        public static bool NewMessage(this User user, int messageLength, List<Pet> availablePets)
        {
            DateTime messageReceivedAt = DateTime.UtcNow;
            ++user.MessageCount;
            user.TotalMessageLength += Convert.ToUInt64(messageLength);

            bool lastMessageWasMoreThanAMinuteAgo = (messageReceivedAt - user.LastXpEarningMessage.GetValueOrDefault()).TotalSeconds >= 60;
            if (lastMessageWasMoreThanAMinuteAgo)
            {
                user.LastXpEarningMessage = messageReceivedAt;
                user.MessageXpEarned += LevellingMaths.ApplyPetBonuses(LevelConfig.MessageXp, availablePets, BonusType.Message);
            }

            user.LastActivity = messageReceivedAt;
            user.LastMessageSent = messageReceivedAt;

            return lastMessageWasMoreThanAMinuteAgo;
        }

        public static void VoiceStateChange(this User user, DiscordVoiceState newState, List<Pet> availablePets)
        {
            DateTime now = DateTime.UtcNow;
            user.LastActivity = now;
            UpdateVoiceCounters(user, now, availablePets);
            UpdateStartTimes(user, newState, now);
        }

        private static void UpdateStartTimes(User user, DiscordVoiceState newState, DateTime now)
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
            }
            else if(newState.Channel == newState.Guild.AfkChannel)
            {
                // User has gone AFK, this doesn't count as being in a normal voice channel.
                user.AfkStartTime = now;
                user.VoiceStartTime = null;
                user.MutedStartTime = null;
                user.DeafenedStartTime = null;
                user.StreamingStartTime = null;
                user.VideoStartTime = null;
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
            }
        }

        private static void UpdateVoiceCounters(User user, DateTime now, List<Pet> availablePets)
        {
            if (user.VoiceStartTime.HasValue)
            {
                var durationDifference = now - user.VoiceStartTime.Value;
                user.TimeSpentInVoice += durationDifference;
                IncrementVoiceXp(user, durationDifference, availablePets);
            }

            if (user.MutedStartTime.HasValue)
            {
                var durationDifference = now - user.MutedStartTime.Value;
                user.TimeSpentMuted += durationDifference;
                IncrementMutedXp(user, durationDifference, availablePets);
            }

            if (user.DeafenedStartTime.HasValue)
            {
                var durationDifference = now - user.DeafenedStartTime.Value;
                user.TimeSpentDeafened += durationDifference;
                IncrementDeafenedXp(user, durationDifference, availablePets);
            }

            if (user.StreamingStartTime.HasValue)
            {
                var durationDifference = now - user.StreamingStartTime.Value;
                user.TimeSpentStreaming += durationDifference;
                IncrementStreamingXp(user, durationDifference, availablePets);
            }

            if (user.VideoStartTime.HasValue)
            {
                var durationDifference = now - user.VideoStartTime.Value;
                user.TimeSpentOnVideo += durationDifference;
                IncrementVideoXp(user, durationDifference, availablePets);
            }

            if (user.AfkStartTime.HasValue)
            {
                user.TimeSpentAfk += (now - user.AfkStartTime.Value);
                // No XP earned or lost for AFK.
            }
        }

        private static void IncrementVoiceXp(User user, TimeSpan voiceDuration, List<Pet> availablePets)
        {
            var baseXp = LevellingMaths.GetDurationXp(voiceDuration, user.TimeSpentInVoice, LevelConfig.VoiceXpPerMin);
            user.VoiceXpEarned += LevellingMaths.ApplyPetBonuses(baseXp, availablePets, BonusType.Voice);
        }

        private static void IncrementMutedXp(User user, TimeSpan voiceDuration, List<Pet> availablePets)
        {
            var baseXp = LevellingMaths.GetDurationXp(voiceDuration, user.TimeSpentMuted, LevelConfig.MutedXpPerMin);
            user.MutedXpEarned += LevellingMaths.ApplyPetBonuses(baseXp, availablePets, BonusType.MutedPentalty);
        }

        private static void IncrementDeafenedXp(User user, TimeSpan voiceDuration, List<Pet> availablePets)
        {
            var baseXp = LevellingMaths.GetDurationXp(voiceDuration, user.TimeSpentDeafened, LevelConfig.DeafenedXpPerMin);
            user.DeafenedXpEarned += LevellingMaths.ApplyPetBonuses(baseXp, availablePets, BonusType.DeafenedPenalty);
        }

        private static void IncrementStreamingXp(User user, TimeSpan voiceDuration, List<Pet> availablePets)
        {
            var baseXp = LevellingMaths.GetDurationXp(voiceDuration, user.TimeSpentStreaming, LevelConfig.StreamingXpPerMin);
            user.StreamingXpEarned += LevellingMaths.ApplyPetBonuses(baseXp, availablePets, BonusType.Streaming);
        }

        private static void IncrementVideoXp(User user, TimeSpan voiceDuration, List<Pet> availablePets)
        {
            var baseXp = LevellingMaths.GetDurationXp(voiceDuration, user.TimeSpentOnVideo, LevelConfig.VideoXpPerMin);
            user.VideoXpEarned += LevellingMaths.ApplyPetBonuses(baseXp, availablePets, BonusType.Video);
        }
    }
}
