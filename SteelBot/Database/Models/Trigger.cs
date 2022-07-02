using SteelBot.Database.Models.Users;
using System;
using System.ComponentModel.DataAnnotations;

namespace SteelBot.Database.Models;

public class Trigger
{
    public long RowId { get; set; }

    [MaxLength(255)]
    public string TriggerText { get; set; }

    public bool ExactMatch { get; set; }

    [MaxLength(255)]
    public string Response { get; set; }

    public DateTime Created { get; set; }

    public long GuildRowId { get; set; }

    public Guild Guild { get; set; }

    public long CreatorRowId { get; set; }
    public User Creator { get; set; }

    public ulong? ChannelDiscordId { get; set; }

    public long TimesActivated { get; set; }

    /// <summary>
    /// Empty constructor.
    /// Used by EF - do not remove.
    /// </summary>
    public Trigger() { }

    public Trigger(string trigger, string response, bool exactMatch = false, ulong? channelId = null)
    {
        TriggerText = trigger;
        Response = response;
        ExactMatch = exactMatch;
        Created = DateTime.UtcNow;
        ChannelDiscordId = channelId;
    }
}