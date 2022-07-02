using System;
using System.Collections.Generic;

namespace SteelBot.Database.Models.Users;

public class User : UserBase
{
    public DateTime UserFirstSeen { get; set; }
    public DateTime? MutedStartTime { get; set; }
    public DateTime? DeafenedStartTime { get; set; }
    public DateTime? StreamingStartTime { get; set; }
    public DateTime? VideoStartTime { get; set; }
    public DateTime? VoiceStartTime { get; set; }
    public DateTime? AfkStartTime { get; set; }
    public DateTime? DisconnectedStartTime { get; set; }
    public DateTime LastActivity { get; set; }
    public DateTime? LastMessageSent { get; set; }
    public DateTime? LastXpEarningMessage { get; set; }
    public Guild Guild { get; set; }
    public RankRole CurrentRankRole { get; set; }
    public List<Trigger> CreatedTriggers { get; set; }
    public DateTime? LastUpdated { get; set; }

    #region TimeSpan Computers

    public TimeSpan TimeSpentInVoice
    {
        get => TimeSpan.FromSeconds(TimeSpentInVoiceSeconds);
        set => TimeSpentInVoiceSeconds = (ulong)Math.Floor(value.TotalSeconds);
    }

    public TimeSpan TimeSpentMuted
    {
        get => TimeSpan.FromSeconds(TimeSpentMutedSeconds);
        set => TimeSpentMutedSeconds = (ulong)Math.Floor(value.TotalSeconds);
    }

    public TimeSpan TimeSpentDeafened
    {
        get => TimeSpan.FromSeconds(TimeSpentDeafenedSeconds);
        set => TimeSpentDeafenedSeconds = (ulong)Math.Floor(value.TotalSeconds);
    }

    public TimeSpan TimeSpentStreaming
    {
        get => TimeSpan.FromSeconds(TimeSpentStreamingSeconds);
        set => TimeSpentStreamingSeconds = (ulong)Math.Floor(value.TotalSeconds);
    }

    public TimeSpan TimeSpentOnVideo
    {
        get => TimeSpan.FromSeconds(TimeSpentOnVideoSeconds);
        set => TimeSpentOnVideoSeconds = (ulong)Math.Floor(value.TotalSeconds);
    }

    public TimeSpan TimeSpentAfk
    {
        get => TimeSpan.FromSeconds(TimeSpentAfkSeconds);
        set => TimeSpentAfkSeconds = (ulong)Math.Floor(value.TotalSeconds);
    }

    public TimeSpan TimeSpentDisconnected
    {
        get => TimeSpan.FromSeconds(TimeSpentDisconnectedSeconds);
        set => TimeSpentDisconnectedSeconds = (ulong)Math.Floor(value.TotalSeconds);
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
        var userCopy = (User)MemberwiseClone();
        return userCopy;
    }
}