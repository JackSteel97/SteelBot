using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.Helpers.Levelling;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SteelBot.Test.DiscordModules.Pets.Helpers;

public class PetMathsTests
{
    [Theory]
    [InlineData(1, Rarity.Common)]
    [InlineData(1, Rarity.Legendary)]
    [InlineData(1, Rarity.Mythical)]
    [InlineData(50, Rarity.Mythical)]
    [InlineData(60, Rarity.Mythical)]
    [InlineData(100, Rarity.Mythical)]
    [InlineData(150, Rarity.Mythical)]
    public void CalculateTreatXp_ShouldBeWithinBounds(int petLevel, Rarity rarity)
    {
        double xpRequiredForNextLevel = LevellingMaths.PetXpForLevel(petLevel + 1, rarity, false);
        double xpRequiredForThisLevel = LevellingMaths.PetXpForLevel(petLevel, rarity, false);
        double xpRequiredToLevel = xpRequiredForNextLevel - xpRequiredForThisLevel;
        var loggerMock = new Mock<ILogger>();
        double xpGain = PetMaths.CalculateTreatXp(petLevel, rarity, 1, false, loggerMock.Object);
        xpGain.Should()
            .BeLessThan(xpRequiredToLevel)
            .And
            .BeGreaterOrEqualTo(100);
    }

    [Theory]
    [InlineData(1, Rarity.Common)]
    [InlineData(1, Rarity.Legendary)]
    [InlineData(1, Rarity.Mythical)]
    [InlineData(50, Rarity.Mythical)]
    [InlineData(60, Rarity.Mythical)]
    [InlineData(100, Rarity.Common)]
    [InlineData(100, Rarity.Mythical)]
    [InlineData(150, Rarity.Mythical)]
    [InlineData(200, Rarity.Mythical)]
    public void CalculateTreatXp_DistributionShouldDeviateByLessThan3Percent(int petLevel, Rarity rarity)
    {
        double xpRequiredForNextLevel = LevellingMaths.PetXpForLevel(petLevel + 1, rarity, false);
        double xpRequiredForThisLevel = LevellingMaths.PetXpForLevel(petLevel, rarity, false);
        double xpRequiredToLevel = xpRequiredForNextLevel - xpRequiredForThisLevel;
        var loggerMock = new Mock<ILogger>();

        const int iterations = 1000;
        var percentBucketCounter = new Dictionary<int, int>();
        for (int i = 0; i < iterations; ++i)
        {
            double xpGain = PetMaths.CalculateTreatXp(petLevel, rarity, 1, false, loggerMock.Object);
            double gainPercent = (xpGain / xpRequiredToLevel) * 100;

            int roundedPercent = (int)Math.Round(gainPercent, MidpointRounding.AwayFromZero);
            if (!percentBucketCounter.ContainsKey(roundedPercent))
            {
                percentBucketCounter.Add(roundedPercent, 1);
            }
            else
            {
                percentBucketCounter[roundedPercent] += 1;
            }
        }

        percentBucketCounter.Count.Should().BeGreaterThan(1);
        StandardDeviation(percentBucketCounter.Values.AsEnumerable())
            .Should().BeLessThan(iterations * 0.03);
    }
    
    private static double StandardDeviation(IEnumerable<int> values)
    {
        double avg = values.Average();
        return Math.Sqrt(values.Average(v=>Math.Pow(v-avg,2)));
    }
}