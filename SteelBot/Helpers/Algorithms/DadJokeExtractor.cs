using System;

namespace SteelBot.Helpers.Algorithms
{
    public static class DadJokeExtractor
    {
        private static string[] possibilities = new string[] { "i'm", "i’m", "i‘m" };

        public static string Extract(string input)
        {
            const string searchString = "i'm";
            int startIndex = FindFirstIndexOfPossibilities(input, possibilities);
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

        private static int FindFirstIndexOfPossibilities(string input, params string[] possibilities)
        {
            foreach (string possibility in possibilities)
            {
                int index = input.IndexOf(possibility, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    return index;
                }
            }
            return -1;
        }
    }
}