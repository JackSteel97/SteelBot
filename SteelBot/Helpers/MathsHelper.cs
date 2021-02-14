using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.Helpers
{
    public static class MathsHelper
    {
        public static decimal PercentageChange(decimal oldValue, decimal newValue)
        {
            if (oldValue != 0)
            {
                return (newValue - oldValue) / Math.Abs(oldValue);
            }
            return newValue / 100;
        }
    }
}