using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.Helpers.Extensions
{
    public static class NumberExtensions
    {
        public static string KiloFormat(this ulong value)
        {
            if (value >= 100000000000)
                return (value / 1000000000).ToString("#,0") + " B";
            if (value >= 10000000000)
                return (value / 1000000000D).ToString("0.#") + " B";
            if (value >= 100000000)
                return (value / 1000000).ToString("#,0") + " M";
            if (value >= 10000000)
                return (value / 1000000D).ToString("0.#") + " M";
            if (value >= 100000)
                return (value / 1000).ToString("#,0") + " K";
            if (value >= 10000)
                return (value / 1000D).ToString("0.#") + " K";
            return value.ToString("#,0");
        }
    }
}