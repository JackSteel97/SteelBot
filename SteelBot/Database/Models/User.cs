using SteelBot.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SteelBot.Database.Models
{
    public class User
    {
        public long RowId { get; set; }
        public ulong DiscordId { get; set; }
        public long MessageCount { get; set; }
        public ulong TotalMessageLength { get; set; }
        public TimeSpan TimeSpentInVoice { get; set; }
        public TimeSpan TimeSpentMuted { get; set; }
        public TimeSpan TimeSpentDeafened { get; set; }
        public TimeSpan TimeSpentStreaming { get; set; }
        public DateTime LastCommandReceived { get; set; }
        public DateTime UserFirstSeen { get; set; }
        public DateTime? MutedStartTime { get; set; }
        public DateTime? DeafenedStartTime { get; set; }
        public DateTime? StreamingStartTime { get; set; }
        public DateTime? VoiceStartTime { get; set; }
        public DateTime LastActivity { get; set; }

        public long GuildRowId { get; set; }
        public Guild Guild { get; set; }

        public ulong MessageXpEarned { get; set; }

        public ulong ActivityXpEarned { get; set; }

        public int CurrentLevel { get; set; }

        public DateTime? LastMessageSent { get; set; }

        public ulong TotalXp
        {
            get
            {
                return MessageXpEarned + ActivityXpEarned;
            }
        }

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
            User userCopy = (User)this.MemberwiseClone();
            return userCopy;
        }

        public long GetAverageMessageLength()
        {
            return Convert.ToInt64(TotalMessageLength) / MessageCount;
        }

        public bool UpdateLevel(bool addMessageXp = false)
        {
            ActivityXpEarned = CalculateCurrentActivityXp();
            if (addMessageXp)
            {
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
            double totalNegativeXp = TimeSpentMuted.TotalMinutes + (TimeSpentDeafened.TotalMinutes * 0.5);
            currentXp += (ulong)Math.Floor(TimeSpentInVoice.TotalMinutes);

            currentXp += (ulong)Math.Floor(TimeSpentStreaming.TotalMinutes * 5);

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
    }
}