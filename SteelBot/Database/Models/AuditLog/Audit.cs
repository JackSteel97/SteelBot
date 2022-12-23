using System;
using System.ComponentModel.DataAnnotations;

namespace SteelBot.Database.Models.AuditLog;

public class Audit
{
    public long RowId { get; set; }
    public ulong Who { get; set; }

    [MaxLength(50)] public string WhoName { get; set; }

    public AuditAction What { get; set; }
    public ulong? WhereGuildId { get; set; }

    [MaxLength(255)] public string WhereGuildName { get; set; }

    public ulong? WhereChannelId { get; set; }

    [MaxLength(255)] public string WhereChannelName { get; set; }

    [MaxLength(2000)] public string Description { get; set; }

    public DateTime When { get; set; }

    /// <summary>
    ///     Used by EF - Do not remove
    /// </summary>
    public Audit() { }

    public Audit(ulong userId, string userName, AuditAction action)
    {
        Who = userId;
        WhoName = userName;
        What = action;
        When = DateTime.UtcNow;
    }

    public Audit(ulong userId, string userName, AuditAction action, ulong guildId, string guildName) : this(userId, userName, action)
    {
        WhereGuildId = guildId;
        WhereGuildName = guildName;
    }

    public Audit(ulong userId, string userName, AuditAction action, ulong guildId, string guildName, ulong channelId, string channelName) : this(userId, userName, action, guildId, guildName)
    {
        WhereChannelId = channelId;
        WhereChannelName = channelName;
    }
}