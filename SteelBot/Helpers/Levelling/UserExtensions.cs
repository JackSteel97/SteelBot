using DSharpPlus.Entities;
using SteelBot.Database.Models;
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

        public static bool NewMessage(this User user, int messageLength)
        {
            DateTime messageReceivedAt = DateTime.UtcNow;
            ++user.MessageCount;
            user.TotalMessageLength += Convert.ToUInt64(messageLength);

            bool lastMessageWasMoreThanAMinuteAgo = (messageReceivedAt - user.LastXpEarningMessage.GetValueOrDefault()).TotalSeconds >= 60;
            if (lastMessageWasMoreThanAMinuteAgo)
            {
                user.LastXpEarningMessage = messageReceivedAt;
                user.MessageXpEarned += LevelConfig.MessageXp;
            }

            user.LastActivity = messageReceivedAt;
            user.LastMessageSent = messageReceivedAt;

            return lastMessageWasMoreThanAMinuteAgo;
        }

        public static void VoiceStateChange(this User user, DiscordVoiceState newState)
        {
            DateTime now = DateTime.UtcNow;
            user.LastActivity = now;
            UpdateVoiceXp(user, now);
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
            }
            else
            {
                // User is in voice channel.
                user.VoiceStartTime = now;
                user.MutedStartTime = newState.IsSelfMuted ? now : null;
                user.DeafenedStartTime = newState.IsSelfDeafened ? now : null;
                user.StreamingStartTime = newState.IsSelfStream ? now : null;
                user.VideoStartTime = newState.IsSelfVideo ? now : null;
            }
        }

        private static void UpdateVoiceXp(User user, DateTime now)
        {
            if (user.VoiceStartTime.HasValue)
            {
                var durationDifference = now - user.VoiceStartTime.Value;
                user.TimeSpentInVoice += durationDifference;
                IncrementVoiceXp(user, durationDifference);
            }

            if (user.MutedStartTime.HasValue)
            {
                var durationDifference = now - user.MutedStartTime.Value;
                user.TimeSpentMuted += durationDifference;
                IncrementMutedXp(user, durationDifference);
            }

            if (user.DeafenedStartTime.HasValue)
            {
                var durationDifference = now - user.DeafenedStartTime.Value;
                user.TimeSpentDeafened += durationDifference;
                IncrementDeafenedXp(user, durationDifference);
            }

            if (user.StreamingStartTime.HasValue)
            {
                var durationDifference = now - user.StreamingStartTime.Value;
                user.TimeSpentStreaming += durationDifference;
                IncrementStreamingXp(user, durationDifference);
            }

            if (user.VideoStartTime.HasValue)
            {
                var durationDifference = now - user.VideoStartTime.Value;
                user.TimeSpentOnVideo += durationDifference;
                IncrementVideoXp(user, durationDifference);
            }
        }

        private static void IncrementVoiceXp(User user, TimeSpan voiceDuration)
        {
            user.VoiceXpEarned += LevellingMaths.GetDurationXp(voiceDuration, LevelConfig.VoiceXpPerMin);
        }

        private static void IncrementMutedXp(User user, TimeSpan voiceDuration)
        {
            user.MutedXpEarned += LevellingMaths.GetDurationXp(voiceDuration, LevelConfig.MutedXpPerMin);
        }

        private static void IncrementDeafenedXp(User user, TimeSpan voiceDuration)
        {
            user.DeafenedXpEarned += LevellingMaths.GetDurationXp(voiceDuration, LevelConfig.DeafenedXpPerMin);
        }

        private static void IncrementStreamingXp(User user, TimeSpan voiceDuration)
        {
            user.StreamingXpEarned += LevellingMaths.GetDurationXp(voiceDuration, LevelConfig.StreamingXpPerMin);
        }

        private static void IncrementVideoXp(User user, TimeSpan voiceDuration)
        {
            user.VideoXpEarned += LevellingMaths.GetDurationXp(voiceDuration, LevelConfig.VideoXpPerMin);
        }
    }
}
