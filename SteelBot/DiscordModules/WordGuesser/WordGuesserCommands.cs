using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Humanizer;
using Microsoft.Extensions.Logging;
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

namespace SteelBot.DiscordModules.WordGuesser
{
    public class WordGuesserStats
    {
        public ulong UserId { get; set; }
        public int Guesses { get; set; }
        public int CorrectGuesses { get; set; }

    }

    [RequireGuild]
    [Group("Words")]
    [Aliases("W", "Word")]
    public class WordGuesserCommands : TypingCommandModule
    {
        private const int MaxOutputLines = 20;

        private readonly ILogger<WordGuesserCommands> Logger;
        private readonly AppConfigurationService AppConfigurationService;
        private readonly HashSet<string> AllWords;
        private int WordCounter = 0;
        private string CurrentWord = "";
        private int GuessCount = 0;
        private int OutputLineCount = 0;
        private Dictionary<char, int> CurrentWordFrequencies = new Dictionary<char, int>();
        private readonly Dictionary<ulong, WordGuesserStats> Stats = new Dictionary<ulong, WordGuesserStats>();
        private readonly StringBuilder CurrentResponse = new StringBuilder();

        public WordGuesserCommands(ILogger<WordGuesserCommands> logger, AppConfigurationService appConfigurationService)
        {
            Logger = logger;
            AppConfigurationService = appConfigurationService;
            AllWords = LoadWords();
            PickNewWord();
        }

        [GroupCommand]
        [Description("Guess the current word")]
        [Cooldown(10, 60, CooldownBucketType.User)]
        public async Task Guess(CommandContext context, [RemainingText] string guess)
        {
            if (string.IsNullOrWhiteSpace(guess) || guess.Length != CurrentWord.Length)
            {
                await context.RespondAsync(EmbedGenerator.Error($"Please guess a {Formatter.Bold(CurrentWord.Length.ToString())} letter word", "Wrong Length"));
                return;
            }

            if (ContainsNonAlphaCharacters(guess))
            {
                await context.RespondAsync(EmbedGenerator.Error("Only letters are allowed in guesses", "Invalid characters"));
                return;
            }

            var result = CheckGuess(guess, out bool correctGuess);
            foreach(var part in result)
            {
                CurrentResponse.Append(part);
            }
            CurrentResponse.Append(" - ").AppendLine(context.User.Mention).AppendLine();
            GuessCount++;
            OutputLineCount++;

            Guess(context.User.Id, wasCorrect: correctGuess);

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();

            if (correctGuess)
            {
                CurrentResponse.AppendLine($"{context.User.Mention} got the correct answer for word **#{WordCounter}** after a total of **{GuessCount}** guesses");
                embedBuilder.WithColor(EmbedGenerator.SuccessColour).WithTitle("Congratulations!").WithDescription(CurrentResponse.ToString());
            }
            else
            {
                if(OutputLineCount > MaxOutputLines)
                {
                    RemoveFirstNLines(CurrentResponse, 2);
                    OutputLineCount--;
                }
                embedBuilder.WithColor(EmbedGenerator.InfoColour).WithTitle($"Word #{WordCounter}").WithDescription(CurrentResponse.ToString());
            }

            await context.RespondAsync(embed: embedBuilder.Build());

            if (correctGuess)
            {
                PickNewWord();
                await context.Channel.SendMessageAsync(EmbedGenerator.Info($"The next word is {CurrentWord.Length} letters long", "New Word!"));
            }
        }

        [Command("Leaderboard")]
        [Description("Show a simple leaderboard")]
        [Cooldown(2, 60, CooldownBucketType.Channel)]
        public async Task Leaderboard(CommandContext context)
        {
            var allStats = Stats.Values.OrderByDescending(x => x.CorrectGuesses).ToArray();

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithColor(EmbedGenerator.InfoColour)
                .WithTitle($"{context.Guild.Name} Word Guesser Leaderboard")
                .WithTimestamp(DateTime.UtcNow);

            List<Page> pages = PaginationHelper.GenerateEmbedPages(embedBuilder, allStats, 10, (builder, user, index) =>
            {
                return builder
                    .AppendLine($"**{(index + 1).Ordinalize()}** - {user.UserId.ToMention()}")
                    .AppendLine($"Guesses `{user.Guesses}`")
                    .AppendLine($"Correct `{$"{user.CorrectGuesses}"}`");
            });

            InteractivityExtension interactivity = context.Client.GetInteractivity();

            await interactivity.SendPaginatedMessageAsync(context.Channel, context.User, pages);
        }

        [Command("Show")]
        [RequireOwner]
        public async Task AdminShowWord(CommandContext context)
        {
            await context.RespondAsync(CurrentWord);
        }

        private HashSet<string> LoadWords()
        {
            var path = Path.Combine(AppConfigurationService.BasePath, "Resources", "WordGuesser", "words_alpha.txt");
            var words = File.ReadAllLines(path);
            HashSet<string> allWords = new HashSet<string>();
            foreach (var word in words)
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
            Dictionary<char, int> correctLetters = BuildLetterDictionary(CurrentWord);
            Dictionary<char, int> maybeLetters = BuildLetterDictionary(CurrentWord);

            // Correct pass.
            string lowerGuess = guess.ToLower();
            for (int i = 0; i < lowerGuess.Length; ++i)
            {
                if (lowerGuess[i] == CurrentWord[i])
                {
                    output[i] = GetDisplayLetterCorrect(lowerGuess[i]);
                    correctLetters[CurrentWord[i]]++;
                }
            }

            // Contains pass.
            for (int i = 0; i < lowerGuess.Length; ++i)
            {
                // If not already correct
                if (output[i] == null)
                {
                    if (CurrentWordFrequencies.ContainsKey(lowerGuess[i])
                        && correctLetters[lowerGuess[i]] < CurrentWordFrequencies[lowerGuess[i]]
                        && maybeLetters[lowerGuess[i]] < CurrentWordFrequencies[lowerGuess[i]])
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
            WordCounter++;
            CurrentResponse.Clear();
            GuessCount = 0;
            OutputLineCount = 0;
            var index = RandomNumberGenerator.GetInt32(AllWords.Count);
            CurrentWord = AllWords.ElementAt(index).ToLower();
            CurrentWordFrequencies = BuildLetterFrequencyDictionary(CurrentWord);
        }

        private void Guess(ulong userId, bool wasCorrect = false)
        {
            if (!Stats.TryGetValue(userId, out var userStats))
            {
                userStats = new WordGuesserStats()
                {
                    UserId = userId
                };
                Stats.Add(userId, userStats);
            }
            userStats.Guesses++;
            if (wasCorrect)
            {
                userStats.CorrectGuesses++;
            }
        }

        private Dictionary<char, int> BuildLetterFrequencyDictionary(string input)
        {
            var result = new Dictionary<char, int>();
            foreach (var character in input)
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


        private Dictionary<char, int> BuildLetterDictionary(string input)
        {
            var result = new Dictionary<char, int>();
            foreach (var character in input)
            {
                if (!result.ContainsKey(character))
                {
                    result.Add(character, 0);
                }
            }

            return result;
        }

        private bool ContainsNonAlphaCharacters(string input)
        {
            foreach(var character in input)
            {
                if (!char.IsLetter(character))
                {
                    return true;
                }
            }

            return false;
        }

        private string GetDisplayLetterCorrect(char character)
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

        private string GetDisplayLetterMaybe(char character)
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

        private string GetDisplayLetterIncorrect(char character)
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

        private StringBuilder RemoveFirstNLines(StringBuilder s, int n)
        {
            var content = s.ToString();
            for(int i = 0; i < n; ++i)
            {
                content = content.Substring(content.IndexOf(Environment.NewLine) + 1);
            }
            s.Clear();
            s.Append(content);
            return s;
        }
    }
}
