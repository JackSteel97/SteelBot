using System;

namespace SteelBot.Database.Models.Users;

/// <summary>
/// Serves as a historical audit entry whenever anything changes on the user.
/// </summary>
public class UserAudit : UserBase
{
    public ulong GuildDiscordId { get; set; }
    public string CurrentRankRoleName { get; set; }
    public DateTime Timestamp { get; set; }
    /// <summary>
    /// Empty constructor.
    /// Used by EF do not remove.
    /// </summary>
    public UserAudit() { }

    public UserAudit(User user, ulong guildDiscordId, string currentRankRoleName = null)
    {
        DiscordId = user.DiscordId;
        GuildRowId = user.GuildRowId;
        MessageCount = user.MessageCount;
        TotalMessageLength = user.TotalMessageLength;
        TimeSpentInVoiceSeconds = user.TimeSpentInVoiceSeconds;
        TimeSpentMutedSeconds = user.TimeSpentMutedSeconds;
        TimeSpentDeafenedSeconds = user.TimeSpentDeafenedSeconds;
        TimeSpentStreamingSeconds = user.TimeSpentStreamingSeconds;
        TimeSpentOnVideoSeconds = user.TimeSpentOnVideoSeconds;
        TimeSpentAfkSeconds = user.TimeSpentAfkSeconds;
        TimeSpentDisconnectedSeconds = user.TimeSpentDisconnectedSeconds;
        MessageXpEarned = user.MessageXpEarned;
        VoiceXpEarned = user.VoiceXpEarned;
        MutedXpEarned = user.MutedXpEarned;
        DeafenedXpEarned = user.DeafenedXpEarned;
        StreamingXpEarned = user.StreamingXpEarned;
        VideoXpEarned = user.VideoXpEarned;
        DisconnectedXpEarned = user.DisconnectedXpEarned;
        CurrentLevel = user.CurrentLevel;
        CurrentRankRoleRowId = user.CurrentRankRoleRowId;
        GuildDiscordId = guildDiscordId;
        CurrentRankRoleName = currentRankRoleName;
        ActivityStreakXpEarned = user.ActivityStreakXpEarned;
        LastActiveDay = user.LastActiveDay;
        ConsecutiveDaysActive = user.ConsecutiveDaysActive;
        Timestamp = user.LastUpdated ?? user.UserFirstSeen;
        OptedOutOfMentions = user.OptedOutOfMentions;
    }
}