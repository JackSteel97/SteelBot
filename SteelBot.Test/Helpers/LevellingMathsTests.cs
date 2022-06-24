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

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 501)]
        [InlineData(2, 1008)]
        [InlineData(3, 1528)]
        [InlineData(4, 2065)]
        [InlineData(5, 2626)]
        [InlineData(10, 6005)]
        [InlineData(50, 159_099)]
        [InlineData(70, 726_888)]
        [InlineData(100, 83_867_974)]
        public void XpForLevel(int level, ulong expectedXp)
        {
            ulong actualXp = LevellingMaths.XpForLevel(level);

            Assert.Equal(expectedXp, actualXp);
        }
    }
}