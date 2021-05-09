using SteelBot.Helpers.Algorithms;
using Xunit;

namespace SteelBot.Test.Helpers.Algorithms
{
    public class DadJokeExtractorTests
    {
        [Theory]
        [InlineData("I'm back", "back")]
        [InlineData("I'm not going out", "not going out")]
        [InlineData("I'm having some fish, for dinner", "having some fish")]
        [InlineData("Some more text that is not relevant. I'm having some fish, for dinner", "having some fish")]
        [InlineData("Some more text that is not relevat. i'm HAVING SOME FISH, for dinner", "HAVING SOME FISH")]
        [InlineData("this is it i'm not having any of this", "not having any of this")]
        [InlineData("this is it i'm", "")]
        [InlineData("this is it i'm,", "")]
        [InlineData("this is it i'm.", "")]
        [InlineData("this is it i'm   ", "")]
        [InlineData("this is it i'm not here   ", "not here")]
        [InlineData("this is it", "")]
        public void Extract(string input, string expectedOutput)
        {
            string actualOutput = DadJokeExtractor.Extract(input);
            Assert.Equal(expectedOutput, actualOutput);
        }
    }
}