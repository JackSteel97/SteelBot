using FluentAssertions;
using SteelBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SteelBot.Test.Helpers.Maths;

public class MathsHelperTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(0.1)]
    [InlineData(0.2)]
    [InlineData(0.3)]
    [InlineData(0.4)]
    [InlineData(0.5)]
    [InlineData(0.6)]
    [InlineData(0.7)]
    [InlineData(0.8)]
    [InlineData(0.9)]
    [InlineData(1)]
    public void TrueWithProbability(double probability)
    {
        int trueCount = 0;
        const int iterations = 1_000_000;
        for (int i = 0; i < iterations; ++i)
        {
            if (MathsHelper.TrueWithProbability(probability))
            {
                ++trueCount;
            }
        }

        int expectedTrue = Convert.ToInt32(probability * iterations);
        uint delta = Convert.ToUInt32(iterations * 0.01); // Within 1%
        trueCount.Should().BeCloseTo(expectedTrue, delta);
    }
}