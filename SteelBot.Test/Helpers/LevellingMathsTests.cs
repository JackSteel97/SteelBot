using SteelBot.Helpers.Levelling;
using System;
using Xunit;

namespace SteelBot.Test.Helpers
{
    public class LevellingMathsTests
    {
        [Theory]
        [InlineData(0, 0, 1, 0)]
        [InlineData(1, 0, 1, 60)]
        [InlineData(168, 0, 1, 10080)] // 1 week, 0 hours
        [InlineData(2, 168, 10, 2400)] // 2 hours, 1 week
        [InlineData(2, 504, 10, 4800)] // 2 hours, 3 weeks
        [InlineData(6, 840, 1, 2160)] // 6 hours, 5 weeks
        public void GetDurationXp(double durationHours, double existingDurationHours, double baseXp, double expectedXp)
        {
            TimeSpan duration = TimeSpan.FromHours(durationHours);
            TimeSpan existingDuration = TimeSpan.FromHours(existingDurationHours);

            double actualXp = LevellingMaths.GetDurationXp(duration, existingDuration, baseXp);

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