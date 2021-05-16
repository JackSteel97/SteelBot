using SteelBot.Helpers.Algorithms;
using Xunit;

namespace SteelBot.Test.Helpers.Algorithms
{
    public class UwuifyerTests
    {
        [Theory]
        [InlineData("I'm back", "back")]
        public void Uwuify(string input, string expectedOutput)
        {
            string actualOutput = Uwuifyer.Uwuify(input);
            Assert.Equal(expectedOutput, actualOutput);
        }
    }
}