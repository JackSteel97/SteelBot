using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.Helpers.Levelling;
using Xunit;

namespace SteelBot.Test.DiscordModules.Pets.Helpers;

public class PetMathsTests
{
    [Theory]
    [InlineData(1, 1, Rarity.Common)]
    [InlineData(1, 1, Rarity.Legendary)]
    [InlineData(1, 1, Rarity.Mythical)]
    [InlineData(50, 40, Rarity.Mythical)]
    [InlineData(60, 50, Rarity.Mythical)]
    [InlineData(100, 110, Rarity.Mythical)]
    [InlineData(150, 175, Rarity.Mythical)]
    public void CalculateTreatXp_ShouldBeWithinBounds(int petLevel, int userLevel, Rarity rarity)
    {
        double xpRequiredForNextLevel = LevellingMaths.PetXpForLevel(petLevel + 1, rarity, false);
        double xpRequiredForThisLevel = LevellingMaths.PetXpForLevel(petLevel, rarity, false);
        double xpRequiredToLevel = xpRequiredForNextLevel - xpRequiredForThisLevel;
        var loggerMock = new Mock<ILogger>();
        double xpGain = PetMaths.CalculateTreatXp(petLevel, rarity, 1, userLevel, loggerMock.Object);
        xpGain.Should()
            .BeLessThan(xpRequiredToLevel * 2)
            .And
            .BeGreaterOrEqualTo(100);
    }
}