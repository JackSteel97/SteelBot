using SteelBot.Helpers;
using SteelBot.Services.Configuration;
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
        public ulong TimeSpentAfkSeconds { get; set; }
        public DateTime UserFirstSeen { get; set; }
        public DateTime? MutedStartTime { get; set; }
        public DateTime? DeafenedStartTime { get; set; }
        public DateTime? StreamingStartTime { get; set; }
        public DateTime? VideoStartTime { get; set; }
        public DateTime? VoiceStartTime { get; set; }
        public DateTime? AfkStartTime { get; set; }
        public DateTime LastActivity { get; set; }

        public long GuildRowId { get; set; }
        public Guild Guild { get; set; }

        public ulong MessageXpEarned { get; set; }
        public ulong VoiceXpEarned { get; set; }
        public ulong MutedXpEarned { get; set; }
        public ulong DeafenedXpEarned { get; set; }
        public ulong StreamingXpEarned { get; set; }
        public ulong VideoXpEarned { get; set; }

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
                var positiveXp = MessageXpEarned + VoiceXpEarned + StreamingXpEarned + VideoXpEarned;
                var negativeXp = MutedXpEarned + DeafenedXpEarned;
                if(positiveXp >= negativeXp)
                {
                    return positiveXp - negativeXp;
                }
                else
                {
                    return 0;
                }
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

        public TimeSpan TimeSpentAfk
        {
            get
            {
                return TimeSpan.FromSeconds(TimeSpentAfkSeconds);
            }
            set
            {
                TimeSpentAfkSeconds = (ulong)Math.Floor(value.TotalSeconds);
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
    }
}