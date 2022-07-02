using System;
using System.ComponentModel.DataAnnotations;

namespace SteelBot.Database.Models;

public class SelfRole
{
    public long RowId { get; set; }

    [MaxLength(255)]
    public string RoleName { get; set; }

    public ulong DiscordRoleId { get; set; }

    public DateTime CreatedAt { get; set; }

    [MaxLength(255)]
    public string Description { get; set; }
    public long GuildRowId { get; set; }
    public Guild Guild { get; set; }

    /// <summary>
    /// Empty constructor.
    /// Used by EF do not remove.
    /// </summary>
    public SelfRole() { }

    public SelfRole(ulong discordId, string roleName, long guildRowId, string description)
    {
        DiscordRoleId = discordId;
        RoleName = roleName;
        CreatedAt = DateTime.UtcNow;
        GuildRowId = guildRowId;
        Description = description;
    }
}