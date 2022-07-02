using System;
using System.Collections.Generic;

namespace SteelBot.Helpers.Algorithms;

public static class DadJokeExtractor
{
    private static readonly string[] _possibilities = new string[] { "i'm", "i’m", "i‘m" };
    private static readonly HashSet<char> _stopCharacters = new HashSet<char> { '.', ',', '?', '!' };

    public static string Extract(string input)
    {
        const string searchString = "i'm";
        int startIndex = FindFirstIndexOfPossibilities(input, _possibilities);
        if (startIndex < 0)
        {
            return string.Empty;
        }
        startIndex += searchString.Length;
        int endIndex;
        for (endIndex = startIndex; endIndex < input.Length; endIndex++)
        {
            char currentChar = input[endIndex];
            if (_stopCharacters.Contains(currentChar))
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