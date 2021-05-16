using SteelBot.Helpers.Algorithms;
using Xunit;

namespace SteelBot.Test.Helpers.Algorithms
{
    public class UwuifyerTests
    {
        [Theory]
        [InlineData("BBC Breaking", "BBC Bweaking")]
        [InlineData("Conflict rages", "Confwict wages")]
        [InlineData("We will continue", "We wiww continue")]
        [InlineData("It was adorable", "It was adowabwe")]
        [InlineData("We emailed the BBC and they have since corrected their article", "We emaiwed da BBC and they haz since cowwected theiw awticwe")]
        public void Uwuify(string input, string expectedOutput)
        {
            string actualOutput = Uwuifyer.Uwuify(input, addFaces: false);
            Assert.Equal(expectedOutput, actualOutput);
        }
    }
}