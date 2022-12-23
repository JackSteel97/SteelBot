using SteelBot.Database.Models.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SteelBot.Database.Models;

public class RankRole
{
    public long RowId { get; set; }
    public ulong RoleDiscordId { get; set; }

    [MaxLength(255)] public string RoleName { get; set; }

    public DateTime CreatedAt { get; set; }
    public long GuildRowId { get; set; }
    public Guild Guild { get; set; }
    public int LevelRequired { get; set; }
    public List<User> UsersWithRole { get; set; }

    /// <summary>
    ///     Empty constructor.
    ///     Used by EF do not remove.
    /// </summary>
    public RankRole() { }

    public RankRole(ulong roleId, string roleName, long guildRowId, int levelRequired)
    {
        RoleDiscordId = roleId;
        RoleName = roleName;
        CreatedAt = DateTime.UtcNow;
        GuildRowId = guildRowId;
        LevelRequired = levelRequired;
    }
}