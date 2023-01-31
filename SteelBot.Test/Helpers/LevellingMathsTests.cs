using SteelBot.Helpers.Levelling;
using System;
using Xunit;

namespace SteelBot.Test.Helpers;

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
        var duration = TimeSpan.FromHours(durationHours);
        var existingDuration = TimeSpan.FromHours(existingDurationHours);

        double actualXp = LevellingMaths.GetDurationXp(duration, existingDuration, baseXp);

        Assert.Equal(expectedXp, actualXp);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 501)]
    [InlineData(2, 1012)]
    [InlineData(3, 1547)]
    [InlineData(4, 2129)]
    [InlineData(5, 2781)]
    [InlineData(10, 8167)]
    [InlineData(50, 917_983)]
    [InlineData(70, 32_53_632)]
    [InlineData(100, 92_867_974)]
    [InlineData(245, 2.508449034568226E+19)]
    public void XpForLevel(int level, double expectedXp)
    {
        double actualXp = LevellingMaths.XpForLevel(level);

        Assert.Equal(expectedXp, actualXp);
    }
}