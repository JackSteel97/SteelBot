using SteelBot.Helpers;
using System;
using System.Collections.Generic;

namespace SteelBot.Database.Models
{
    public class User
    {
        public long RowId { get; set; }
        public ulong DiscordId { get; set; }
        public long MessageCount { get; set; }
        public ulong TotalMessageLength { get; set; }
        public ulong TimeSpentInVoiceSeconds { get; set; }
        public ulong TimeSpentMutedSeconds { get; set; }
        public ulong TimeSpentDeafenedSeconds { get; set; }
        public ulong TimeSpentStreamingSeconds { get; set; }
        public ulong TimeSpentOnVideoSeconds { get; set; }
        public DateTime UserFirstSeen { get; set; }
        public DateTime? MutedStartTime { get; set; }
        public DateTime? DeafenedStartTime { get; set; }
        public DateTime? StreamingStartTime { get; set; }
        public DateTime? VideoStartTime { get; set; }
        public DateTime? VoiceStartTime { get; set; }
        public DateTime LastActivity { get; set; }

        public long GuildRowId { get; set; }
        public Guild Guild { get; set; }

        public ulong MessageXpEarned { get; set; }

        public ulong ActivityXpEarned { get; set; }

        public int CurrentLevel { get; set; }

        public DateTime? LastMessageSent { get; set; }
        public DateTime? LastXpEarningMessage { get; set; }

        public long? CurrentRankRoleRowId { get; set; }

        public RankRole CurrentRankRole { get; set; }
        public List<Trigger> CreatedTriggers { get; set; }
        public StockPortfolio StockPortfolio { get; set; }

        public ulong TotalXp
        {
            get
            {
                return MessageXpEarned + ActivityXpEarned;
            }
        }

        #region TimeSpan Computers

        public TimeSpan TimeSpentInVoice
        {
            get
            {
                return TimeSpan.FromSeconds(TimeSpentInVoiceSeconds);
            }
            set
            {
                TimeSpentInVoiceSeconds = (ulong)Math.Floor(value.TotalSeconds);
            }
        }

        public TimeSpan TimeSpentMuted
        {
            get
            {
                return TimeSpan.FromSeconds(TimeSpentMutedSeconds);
            }
            set
            {
                TimeSpentMutedSeconds = (ulong)Math.Floor(value.TotalSeconds);
            }
        }

        public TimeSpan TimeSpentDeafened
        {
            get
            {
                return TimeSpan.FromSeconds(TimeSpentDeafenedSeconds);
            }
            set
            {
                TimeSpentDeafenedSeconds = (ulong)Math.Floor(value.TotalSeconds);
            }
        }

        public TimeSpan TimeSpentStreaming
        {
            get
            {
                return TimeSpan.FromSeconds(TimeSpentStreamingSeconds);
            }
            set
            {
                TimeSpentStreamingSeconds = (ulong)Math.Floor(value.TotalSeconds);
            }
        }

        public TimeSpan TimeSpentOnVideo
        {
            get
            {
                return TimeSpan.FromSeconds(TimeSpentOnVideoSeconds);
            }
            set
            {
                TimeSpentOnVideoSeconds = (ulong)Math.Floor(value.TotalSeconds);
            }
        }

        #endregion TimeSpan Computers

        /// <summary>
        ///  Empty constructor.
        ///  Used by EF do not remove.
        /// </summary>
        public User() { }

        public User(ulong userId, long guildRowId)
        {
            DiscordId = userId;
            GuildRowId = guildRowId;
            UserFirstSeen = DateTime.UtcNow;
        }

        public User Clone()
        {
            User userCopy = (User)MemberwiseClone();
            return userCopy;
        }

        public long GetAverageMessageLength()
        {
            if (MessageCount > 0)
            {
                return Convert.ToInt64(TotalMessageLength) / MessageCount;
            }
            return 0;
        }

        public float GetMessageEfficiency()
        {
            float minimumMessageCount = (MessageXpEarned / 15f);
            float efficiency = minimumMessageCount / MessageCount;

            return efficiency;
        }

        public bool UpdateLevel(bool addMessageXp = false)
        {
            ActivityXpEarned = CalculateCurrentActivityXp();
            if (addMessageXp)
            {
                LastXpEarningMessage = DateTime.UtcNow;
                MessageXpEarned += 15;
            }

            int newLevel = CurrentLevel;

            bool hasEnoughXp;
            do
            {
                ulong requiredXp = LevellingMaths.XpForLevel(newLevel + 1);
                hasEnoughXp = TotalXp >= requiredXp;
                if (hasEnoughXp)
                {
                    newLevel++;
                }
            } while (hasEnoughXp);

            bool levelIncreased = newLevel > CurrentLevel;
            if (levelIncreased)
            {
                CurrentLevel = newLevel;
            }
            return levelIncreased;
        }

        private ulong CalculateCurrentActivityXp()
        {
            ulong currentXp = 0;
            double totalNegativeXp = TimeSpentMuted.TotalMinutes + (TimeSpentDeafened.TotalMinutes * 0.5 * MathsHelper.GetMultiplier(TimeSpentInVoice));
            currentXp += (ulong)Math.Floor(GetDurationXp(TimeSpentInVoice, 1));

            currentXp += (ulong)Math.Floor(TimeSpentStreaming.TotalMinutes * 5 * MathsHelper.GetMultiplier(TimeSpentStreaming));
            currentXp += (ulong)Math.Floor(TimeSpentOnVideo.TotalMinutes * 10 * MathsHelper.GetMultiplier(TimeSpentOnVideo));

            if (totalNegativeXp < currentXp)
            {
                currentXp -= (ulong)Math.Floor(totalNegativeXp);
            }
            else
            {
                currentXp = 0;
            }

            return currentXp;
        }

        private double GetDurationXp(TimeSpan duration, double baseXp)
        {
            TimeSpan AWeek = TimeSpan.FromDays(7);

            int weeks = (int)Math.Ceiling(duration.TotalDays / 7);

            double totalXp = 0;
            TimeSpan remainingTime = duration;
            for (int week = 1; week <= weeks; week++)
            {
                if (remainingTime.TotalMinutes < AWeek.TotalMinutes)
                {
                    totalXp += remainingTime.TotalMinutes * baseXp * week;
                }
                else
                {
                    totalXp += AWeek.TotalMinutes * baseXp * week;
                    remainingTime = remainingTime.Subtract(AWeek);
                }
            }
            return totalXp;
        }
    }
}