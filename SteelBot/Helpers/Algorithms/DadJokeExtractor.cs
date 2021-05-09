using System;

namespace SteelBot.Helpers.Algorithms
{
    public static class DadJokeExtractor
    {
        public static string Extract(string input)
        {
            const string searchString = "i'm";
            int startIndex = input.IndexOf(searchString, StringComparison.OrdinalIgnoreCase);
            if (startIndex < 0)
            {
                return string.Empty;
            }
            startIndex += searchString.Length;
            int endIndex;
            for (endIndex = startIndex; endIndex < input.Length; endIndex++)
            {
                char currentChar = input[endIndex];
                if (!char.IsLetterOrDigit(currentChar) && !char.IsWhiteSpace(currentChar))
                {
                    break;
                }
            }
            string result = input[startIndex..endIndex].Trim();
            return result;
        }
    }
}