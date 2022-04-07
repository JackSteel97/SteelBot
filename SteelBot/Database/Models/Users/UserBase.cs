using System;

namespace SteelBot.Database.Models.Users
{
    public abstract class UserBase
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
        public ulong TimeSpentDisconnectedSeconds { get; set; }
        public long GuildRowId { get; set; }
        public ulong MessageXpEarned { get; set; }
        public ulong VoiceXpEarned { get; set; }
        public ulong MutedXpEarned { get; set; }
        public ulong DeafenedXpEarned { get; set; }
        public ulong StreamingXpEarned { get; set; }
        public ulong VideoXpEarned { get; set; }
        public ulong DisconnectedXpEarned { get; set; }
        public int CurrentLevel { get; set; }
        public long? CurrentRankRoleRowId { get; set; }

        public ulong TotalXp
        {
            get
            {
                var positiveXp = MessageXpEarned + VoiceXpEarned + StreamingXpEarned + VideoXpEarned + DisconnectedXpEarned;
                var negativeXp = MutedXpEarned + DeafenedXpEarned;
                if (positiveXp >= negativeXp)
                {
                    return positiveXp - negativeXp;
                }
                else
                {
                    return 0;
                }
            }
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
