using DSharpPlus.Entities;
using SteelBot.Database.Models.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SteelBot.Database.Models;

public class Guild
{
    public long RowId { get; set; }

    public ulong DiscordId { get; set; }

    public DateTime BotAddedTo { get; set; }

    public List<User> UsersInGuild { get; set; }
    public List<SelfRole> SelfRoles { get; set; }
    public List<RankRole> RankRoles { get; set; }

    public List<Trigger> Triggers { get; set; }

    [MaxLength(20)]
    public string CommandPrefix { get; set; }

    public ulong? LevelAnnouncementChannelId { get; set; }

    public int GoodBotVotes { get; set; }
    public int BadBotVotes { get; set; }
    public string Name { get; set; }

    /// <summary>
    /// Empty constructor.
    /// Do not remove - used by EF.
    /// </summary>
    public Guild() { }

    public Guild(ulong discordId, string name)
    {
        DiscordId = discordId;
        BotAddedTo = DateTime.UtcNow;
    }

    public Guild Clone()
    {
        var guildCopy = (Guild)MemberwiseClone();
        return guildCopy;
    }

    public DiscordChannel GetLevelAnnouncementChannel(DiscordGuild discordGuild)
    {
        return LevelAnnouncementChannelId.HasValue ? discordGuild.GetChannel(LevelAnnouncementChannelId.Value) : (discordGuild?.SystemChannel);
    }
}