using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Enums
{
    public enum BonusType
    {
        Message,
        Voice,
        Streaming,
        Video,
        MutedPentalty,
        DeafenedPenalty,
        All
    }

    public static class BonusTypeExtensions
    {
        public static bool IsNegative(this BonusType type)
        {
            return type == BonusType.MutedPentalty || type == BonusType.DeafenedPenalty;
        }
    }
}
