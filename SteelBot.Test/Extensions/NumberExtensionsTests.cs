using FluentAssertions;
using SteelBot.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SteelBot.Test.Extensions;

public class NumberExtensionsTests
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
    [InlineData(1000, "1,000")]
    [InlineData(1450, "1,450")]
    [InlineData(10_450, "10.45K")]
    [InlineData(10_453, "10.45K")]
    [InlineData(100_453, "100K")]
    [InlineData(100_700, "101K")]
    [InlineData(997_000, "997K")]
    [InlineData(1_000_000, "1M")]
    [InlineData(1_400_000, "1.4M")]
    [InlineData(1_450_000, "1.45M")]
    [InlineData(10_000_000, "10M")]
    [InlineData(10_400_000, "10.4M")]
    [InlineData(10_450_000, "10.45M")]
    [InlineData(100_000_000, "100M")]
    [InlineData(100_400_000, "100.4M")]
    [InlineData(100_450_000, "100.5M")]
    [InlineData(100_600_000, "100.6M")]
    [InlineData(1_000_000_000, "1B")]
    [InlineData(1_400_000_000, "1.4B")]
    [InlineData(1_450_000_000, "1.45B")]
    [InlineData(10_000_000_000, "10B")]
    [InlineData(10_400_000_000, "10.4B")]
    [InlineData(10_450_000_000, "10.45B")]
    [InlineData(100_000_000_000, "100B")]
    [InlineData(100_400_000_000, "100.4B")]
    [InlineData(100_450_000_000, "100.5B")]
    public void KiloFormat(ulong input, string expectedOutput)
    {
        string actualOutput = NumberExtensions.KiloFormat(input);
        actualOutput.Should().Be(expectedOutput);
    }
}