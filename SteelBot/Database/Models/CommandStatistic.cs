using System;

namespace SteelBot.Database.Models;

public class CommandStatistic
{
    public long RowId { get; set; }
    public string CommandName { get; set; }
    public long UsageCount { get; set; }
    public DateTime LastUsed { get; set; }

    /// <summary>
    ///     Empty constructor.
    ///     Used by EF - do not remove.
    /// </summary>
    public CommandStatistic() { }

    public CommandStatistic(string commandName)
    {
        CommandName = commandName;
        UsageCount = 1;
        LastUsed = DateTime.UtcNow;
    }

    public CommandStatistic Clone() => (CommandStatistic)MemberwiseClone();
}