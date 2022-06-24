using DSharpPlus.Entities;
using SteelBot.Database.Models.Pets;
using SteelBot.Database.Models.Users;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Helpers;
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
            var levelIncreased = LevellingMaths.UpdateLevel(user.CurrentLevel, user.TotalXp, out var newLevel);

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
                user.MessageXpEarned += LevellingMaths.ApplyPetBonuses(LevelConfig.MessageXp, availablePets, BonusType.MessageXP);
            }

            user.LastActivity = messageReceivedAt;
            user.LastMessageSent = messageReceivedAt;

            return lastMessageWasMoreThanAMinuteAgo;
        }

        public static void VoiceStateChange(this User user, DiscordVoiceState newState, List<Pet> availablePets, double scalingFactor, bool shouldEarnVideoXp, bool updateLastActivity = true)
        {
            DateTime now = DateTime.UtcNow;
            if (updateLastActivity)
            {
                user.LastActivity = now;
            }
            UpdateVoiceCounters(user, now, availablePets, scalingFactor, shouldEarnVideoXp);
            UpdateStartTimes(user, newState, now, scalingFactor);
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
                if (newState.Channel == newState.Guild.AfkChannel)
                {
                    user.AfkStartTime = now;
                }
                else
                {
                    user.AfkStartTime = null;
                }
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

            if (user.AfkStartTime.HasValue)
            {
                user.TimeSpentAfk += (now - user.AfkStartTime.Value);
                // No XP earned or lost for AFK.
            }

            if (user.DisconnectedStartTime.HasValue)
            {
                var durationDifference = (now - user.DisconnectedStartTime.Value);
                user.TimeSpentDisconnected += durationDifference;
                IncrementDisconnectedXp(user, durationDifference, availablePets);
            }
        }

        private static void IncrementVoiceXp(User user, TimeSpan voiceDuration, List<Pet> availablePets, double scalingFactor)
        {
            if (scalingFactor != 0)
            {
                var baseXp = LevellingMaths.GetDurationXp(voiceDuration, user.TimeSpentInVoice, LevelConfig.VoiceXpPerMin);
                var bonusAppliedXp = LevellingMaths.ApplyPetBonuses(baseXp, availablePets, BonusType.VoiceXP);
                user.VoiceXpEarned += LevellingMaths.ApplyScalingFactor(bonusAppliedXp, scalingFactor);
            }
        }

        private static void IncrementMutedXp(User user, TimeSpan mutedDuration, List<Pet> availablePets, double scalingFactor)
        {
            if (scalingFactor != 0)
            {
                var baseXp = LevellingMaths.GetDurationXp(mutedDuration, user.TimeSpentMuted, LevelConfig.MutedXpPerMin);
                var bonusAppliedXp = LevellingMaths.ApplyPetBonuses(baseXp, availablePets, BonusType.MutedPenaltyXP);
                user.MutedXpEarned += LevellingMaths.ApplyScalingFactor(bonusAppliedXp, scalingFactor);
            }
        }

        private static void IncrementDeafenedXp(User user, TimeSpan deafenedXp, List<Pet> availablePets, double scalingFactor)
        {
            if (scalingFactor != 0)
            {
                var baseXp = LevellingMaths.GetDurationXp(deafenedXp, user.TimeSpentDeafened, LevelConfig.DeafenedXpPerMin);
                var bonusAppliedXp = LevellingMaths.ApplyPetBonuses(baseXp, availablePets, BonusType.DeafenedPenaltyXP);
                user.DeafenedXpEarned += LevellingMaths.ApplyScalingFactor(bonusAppliedXp, scalingFactor);
            }
        }

        private static void IncrementStreamingXp(User user, TimeSpan streamingDuration, List<Pet> availablePets, double scalingFactor)
        {
            if (scalingFactor != 0)
            {
                var baseXp = LevellingMaths.GetDurationXp(streamingDuration, user.TimeSpentStreaming, LevelConfig.StreamingXpPerMin);
                var bonusAppliedXp = LevellingMaths.ApplyPetBonuses(baseXp, availablePets, BonusType.StreamingXP);
                user.StreamingXpEarned += LevellingMaths.ApplyScalingFactor(bonusAppliedXp, scalingFactor);
            }
        }

        private static void IncrementVideoXp(User user, TimeSpan videoDuration, List<Pet> availablePets, double scalingFactor, bool shouldEarnVideoXp)
        {
            if (scalingFactor != 0 && shouldEarnVideoXp)
            {
                var baseXp = LevellingMaths.GetDurationXp(videoDuration, user.TimeSpentOnVideo, LevelConfig.VideoXpPerMin);
                var bonusAppliedXp = LevellingMaths.ApplyPetBonuses(baseXp, availablePets, BonusType.VideoXP);
                user.VideoXpEarned += LevellingMaths.ApplyScalingFactor(bonusAppliedXp, scalingFactor);
            }
        }

        private static void IncrementDisconnectedXp(User user, TimeSpan disconnectedDuration, List<Pet> availablePets)
        {
            var disconnectedXpPerMin = PetShared.GetBonusValue(availablePets, BonusType.OfflineXP);
            if (disconnectedXpPerMin > 0)
            {
                var xpEarned = LevellingMaths.GetDurationXp(disconnectedDuration, TimeSpan.Zero, disconnectedXpPerMin);
                user.DisconnectedXpEarned += xpEarned;
            }
        }
    }
}
