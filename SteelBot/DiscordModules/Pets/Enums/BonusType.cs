using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Enums
{
    [Flags]
    public enum BonusType
    {
        None = 0,
        Message = 1,
        Voice = 1 << 1,
        Streaming = 1 << 2,
        Video = 1 << 3,
        MutedPentalty = 1 << 4,
        DeafenedPenalty = 1 << 5,
        All = Message | Voice | Streaming | Video
    }

    public static class BonusTypeExtensions
    {
        public static bool IsNegative(this BonusType type)
        {
            return type.HasFlag(BonusType.MutedPentalty) || type.HasFlag(BonusType.DeafenedPenalty);
        }
    }
}
