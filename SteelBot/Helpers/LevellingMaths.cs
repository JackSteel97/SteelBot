using System;
using System.Collections.Generic;
using System.Text;

namespace SteelBot.Helpers
{
    public static class LevellingMaths
    {
        public static ulong XpForLevel(int level)
        {
            // Xp = (1.2^level) + (500*level)
            return Convert.ToUInt64(Math.Round(Math.Pow(1.2, level) + (500 * level)));
        }
    }
}