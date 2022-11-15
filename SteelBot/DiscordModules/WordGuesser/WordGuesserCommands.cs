using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Humanizer;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.WordGuesser;

public class WordGuesserStats
{
    public ulong UserId { get; set; }
    public int Guesses { get; set; }
    public int CorrectGuesses { get; set; }
}

[RequireGuild]
[Group("Words")]
[Aliases("W", "Word")]
[Description("A Wordle style word game")]
public class WordGuesserCommands : TypingCommandModule
{
    private const int _maxOutputLines = 20;

    private readonly ILogger<WordGuesserCommands> _logger;
    private readonly AppConfigurationService _appConfigurationService;
    private readonly HashSet<string> _allWords;
    private int _wordCounter = 0;
    private string _currentWord = "";
    private int _guessCount = 0;
    private int _outputLineCount = 0;
    private Dictionary<char, int> _currentWordFrequencies = new Dictionary<char, int>();
    private readonly Dictionary<ulong, WordGuesserStats> _stats = new Dictionary<ulong, WordGuesserStats>();
    private readonly StringBuilder _currentResponse = new StringBuilder();

    public WordGuesserCommands(ILogger<WordGuesserCommands> logger, AppConfigurationService appConfigurationService, IHub sentry)
        : base(logger, sentry)
    {
        _logger = logger;
        _appConfigurationService = appConfigurationService;
        _allWords = LoadWords();
        PickNewWord();
    }

    [GroupCommand]
    [Description("Guess the current word")]
    [Cooldown(10, 60, CooldownBucketType.User)]
    public async Task Guess(CommandContext context, [RemainingText] string guess)
    {
        if (string.IsNullOrWhiteSpace(guess) || guess.Length != _currentWord.Length)
        {
            _logger.LogWarning("Invalid Guess command request, guess {Guess} is not the same length as the target word", guess);
            await context.RespondAsync(EmbedGenerator.Error($"Please guess a {Formatter.Bold(_currentWord.Length.ToString())} letter word", "Wrong Length"));
            return;
        }

        if (ContainsNonAlphaCharacters(guess))
        {
            _logger.LogWarning("Invalid Guess command request, guess {Guess} contains non-alpha characters", guess);
            await context.RespondAsync(EmbedGenerator.Error("Only letters are allowed in guesses", "Invalid characters"));
            return;
        }

        string[] result = CheckGuess(guess, out bool correctGuess);
        foreach (string part in result)
        {
            _currentResponse.Append(part);
        }
        _currentResponse.Append(" - ").AppendLine(context.User.Mention).AppendLine();
        _guessCount++;
        _outputLineCount++;

        Guess(context.User.Id, wasCorrect: correctGuess);

        var embedBuilder = new DiscordEmbedBuilder();

        if (correctGuess)
        {
            _currentResponse.AppendLine($"{context.User.Mention} got the correct answer for word **#{_wordCounter}** after a total of **{_guessCount}** guesses");
            embedBuilder.WithColor(EmbedGenerator.SuccessColour).WithTitle("Congratulations!").WithDescription(_currentResponse.ToString());
        }
        else
        {
            if (_outputLineCount > _maxOutputLines)
            {
                RemoveFirstNLines(_currentResponse, 2);
                _outputLineCount--;
            }
            embedBuilder.WithColor(EmbedGenerator.InfoColour).WithTitle($"Word #{_wordCounter}").WithDescription(_currentResponse.ToString());
        }

        await context.RespondAsync(embed: embedBuilder.Build());

        if (correctGuess)
        {
            PickNewWord();
            await context.Channel.SendMessageAsync(EmbedGenerator.Info($"The next word is {_currentWord.Length} letters long", "New Word!"));
        }
    }

    [Command("Leaderboard")]
    [Description("Show a simple leaderboard")]
    [Cooldown(2, 60, CooldownBucketType.Channel)]
    public async Task Leaderboard(CommandContext context)
    {
        var allStats = _stats.Values.OrderByDescending(x => x.CorrectGuesses).ToArray();

        var embedBuilder = new DiscordEmbedBuilder()
            .WithColor(EmbedGenerator.InfoColour)
            .WithTitle($"{context.Guild.Name} Word Guesser Leaderboard")
            .WithTimestamp(DateTime.UtcNow);

        var pages = PaginationHelper.GenerateEmbedPages(embedBuilder, allStats, 10, (builder, user, index) =>
        {
            return builder
                .AppendLine($"**{(index + 1).Ordinalize()}** - {user.UserId.ToUserMention()}")
                .AppendLine($"Guesses `{user.Guesses}`")
                .AppendLine($"Correct `{$"{user.CorrectGuesses}"}`");
        });

        var interactivity = context.Client.GetInteractivity();

        await interactivity.SendPaginatedMessageAsync(context.Channel, context.User, pages);
    }

    [Command("Show")]
    [RequireOwner]
    public async Task AdminShowWord(CommandContext context) => await context.RespondAsync(_currentWord);

    private HashSet<string> LoadWords()
    {
        string path = Path.Combine(_appConfigurationService.BasePath, "Resources", "WordGuesser", "words_alpha.txt");
        string[] words = File.ReadAllLines(path);
        var allWords = new HashSet<string>();
        foreach (string word in words)
        {
            if (word.Length >= 4 && word.Length <= 8)
            {
                allWords.Add(word);
            }
        }
        return allWords;
    }

    private string[] CheckGuess(string guess, out bool isCorrect)
    {
        isCorrect = true;
        string[] output = new string[guess.Length];
        var correctLetters = BuildLetterDictionary(_currentWord);
        var maybeLetters = BuildLetterDictionary(_currentWord);

        // Correct pass.
        string lowerGuess = guess.ToLower();
        for (int i = 0; i < lowerGuess.Length; ++i)
        {
            if (lowerGuess[i] == _currentWord[i])
            {
                output[i] = GetDisplayLetterCorrect(lowerGuess[i]);
                correctLetters[_currentWord[i]]++;
            }
        }

        // Contains pass.
        for (int i = 0; i < lowerGuess.Length; ++i)
        {
            // If not already correct
            if (output[i] == null)
            {
                if (_currentWordFrequencies.ContainsKey(lowerGuess[i])
                    && correctLetters[lowerGuess[i]] < _currentWordFrequencies[lowerGuess[i]]
                    && maybeLetters[lowerGuess[i]] < _currentWordFrequencies[lowerGuess[i]])
                {

                    output[i] = GetDisplayLetterMaybe(lowerGuess[i]);
                    maybeLetters[lowerGuess[i]]++;
                }
                else
                {
                    output[i] = GetDisplayLetterIncorrect(lowerGuess[i]);
                }
                isCorrect = false;
            }
        }

        return output;
    }

    private void PickNewWord()
    {
        _wordCounter++;
        _currentResponse.Clear();
        _guessCount = 0;
        _outputLineCount = 0;
        int index = RandomNumberGenerator.GetInt32(_allWords.Count);
        _currentWord = _allWords.ElementAt(index).ToLower();
        _currentWordFrequencies = BuildLetterFrequencyDictionary(_currentWord);
    }

    private void Guess(ulong userId, bool wasCorrect = false)
    {
        if (!_stats.TryGetValue(userId, out var userStats))
        {
            userStats = new WordGuesserStats()
            {
                UserId = userId
            };
            _stats.Add(userId, userStats);
        }
        userStats.Guesses++;
        if (wasCorrect)
        {
            userStats.CorrectGuesses++;
        }
    }

    private static Dictionary<char, int> BuildLetterFrequencyDictionary(string input)
    {
        var result = new Dictionary<char, int>();
        foreach (char character in input)
        {
            if (!result.ContainsKey(character))
            {
                result.Add(character, 1);
            }
            else
            {
                ++result[character];
            }
        }

        return result;
    }

    private static Dictionary<char, int> BuildLetterDictionary(string input)
    {
        var result = new Dictionary<char, int>();
        foreach (char character in input)
        {
            if (!result.ContainsKey(character))
            {
                result.Add(character, 0);
            }
        }

        return result;
    }

    private static bool ContainsNonAlphaCharacters(string input)
    {
        foreach (char character in input)
        {
            if (!char.IsLetter(character))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetDisplayLetterCorrect(char character)
    {
        return character switch
        {
            'a' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.A,
            'b' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.B,
            'c' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.C,
            'd' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.D,
            'e' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.E,
            'f' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.F,
            'g' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.G,
            'h' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.H,
            'i' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.I,
            'j' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.J,
            'k' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.K,
            'l' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.L,
            'm' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.M,
            'n' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.N,
            'o' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.O,
            'p' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.P,
            'q' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.Q,
            'r' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.R,
            's' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.S,
            't' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.T,
            'u' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.U,
            'v' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.V,
            'w' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.W,
            'x' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.X,
            'y' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.Y,
            'z' => EmojiConstants.CustomDiscordEmojis.GreenRegionalIndicators.Z,
            _ => EmojiConstants.Symbols.GreySquare
        };
    }

    private static string GetDisplayLetterMaybe(char character)
    {
        return character switch
        {
            'a' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.A,
            'b' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.B,
            'c' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.C,
            'd' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.D,
            'e' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.E,
            'f' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.F,
            'g' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.G,
            'h' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.H,
            'i' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.I,
            'j' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.J,
            'k' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.K,
            'l' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.L,
            'm' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.M,
            'n' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.N,
            'o' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.O,
            'p' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.P,
            'q' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.Q,
            'r' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.R,
            's' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.S,
            't' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.T,
            'u' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.U,
            'v' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.V,
            'w' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.W,
            'x' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.X,
            'y' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.Y,
            'z' => EmojiConstants.CustomDiscordEmojis.YellowRegionalIndicators.Z,
            _ => EmojiConstants.Symbols.YellowSquare
        };
    }

    private static string GetDisplayLetterIncorrect(char character)
    {
        return character switch
        {
            'a' => EmojiConstants.RegionalIndicators.A,
            'b' => EmojiConstants.RegionalIndicators.B,
            'c' => EmojiConstants.RegionalIndicators.C,
            'd' => EmojiConstants.RegionalIndicators.D,
            'e' => EmojiConstants.RegionalIndicators.E,
            'f' => EmojiConstants.RegionalIndicators.F,
            'g' => EmojiConstants.RegionalIndicators.G,
            'h' => EmojiConstants.RegionalIndicators.H,
            'i' => EmojiConstants.RegionalIndicators.I,
            'j' => EmojiConstants.RegionalIndicators.J,
            'k' => EmojiConstants.RegionalIndicators.K,
            'l' => EmojiConstants.RegionalIndicators.L,
            'm' => EmojiConstants.RegionalIndicators.M,
            'n' => EmojiConstants.RegionalIndicators.N,
            'o' => EmojiConstants.RegionalIndicators.O,
            'p' => EmojiConstants.RegionalIndicators.P,
            'q' => EmojiConstants.RegionalIndicators.Q,
            'r' => EmojiConstants.RegionalIndicators.R,
            's' => EmojiConstants.RegionalIndicators.S,
            't' => EmojiConstants.RegionalIndicators.T,
            'u' => EmojiConstants.RegionalIndicators.U,
            'v' => EmojiConstants.RegionalIndicators.V,
            'w' => EmojiConstants.RegionalIndicators.W,
            'x' => EmojiConstants.RegionalIndicators.X,
            'y' => EmojiConstants.RegionalIndicators.Y,
            'z' => EmojiConstants.RegionalIndicators.Z,
            _ => EmojiConstants.Symbols.GreySquare
        };
    }

    private static StringBuilder RemoveFirstNLines(StringBuilder s, int n)
    {
        string content = s.ToString();
        for (int i = 0; i < n; ++i)
        {
            content = content[(content.IndexOf(Environment.NewLine) + 1)..];
        }
        s.Clear();
        s.Append(content);
        return s;
    }
}