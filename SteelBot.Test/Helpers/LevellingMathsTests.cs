using SteelBot.Database.Models;
using SteelBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SteelBot.Test.Helpers
{
    public class LevellingMathsTests
    {
        [Theory]
        [InlineData(0, 1, 0)]
        [InlineData(1, 1, 60)]
        [InlineData(168, 1, 10080)] // 1 week
        [InlineData(170, 10, 103200)] // 1 week, 2 hours
        [InlineData(992, 1, 205920)] // 5 weeks, 152 hours
        public void GetDurationXp(double durationHours, double baseXp, double expectedXp)
        {
            TimeSpan duration = TimeSpan.FromHours(durationHours);

            double actualXp = LevellingMaths.GetDurationXp(duration, baseXp);

            Assert.Equal(expectedXp, actualXp);
        }

        [Theory] // Xp = (1.2^level)-1 + (500*level)
        [InlineData(0, 0)]
        [InlineData(1, 500)]
        [InlineData(2, 1000)]
        [InlineData(3, 1501)]
        [InlineData(4, 2001)]
        [InlineData(5, 2501)]
        [InlineData(10, 5005)]
        [InlineData(50, 34099)]
        [InlineData(70, 383888)]
        [InlineData(100, 82867974)]
        public void XpForLevel(int level, ulong expectedXp)
        {
            ulong actualXp = LevellingMaths.XpForLevel(level);

            Assert.Equal(expectedXp, actualXp);
        }
    }
}