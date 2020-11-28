using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using SteelBot.Attributes;
using SteelBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Humanizer;
using DSharpPlus.Entities;
using System.Reflection;
using SteelBot.Services.Configuration;
using System.IO;

namespace SteelBot.DiscordModules.Secret
{
    [RequireGuild]
    [GuildCheck(782237087352356876)]
    public class PuzzleCommands : BaseCommandModule
    {
        private const int NumberOfQuestions = 7;
        private readonly List<string> NumberWords;
        private readonly AppConfigurationService AppConfigurationService;

        public PuzzleCommands(AppConfigurationService appConfigurationService)
        {
            NumberWords = new List<string>();
            for (int i = 1; i <= NumberOfQuestions; i++)
            {
                NumberWords.Add(i.ToWords());
            }
            AppConfigurationService = appConfigurationService;
        }

        [Command("Puzzle")]
        [Aliases("Question")]
        [Description("Get the current puzzle.")]
        public async Task Puzzle(CommandContext context)
        {
            int puzzleIndex = NumberWords.FindIndex(word => word.Equals(context.Channel.Name, StringComparison.OrdinalIgnoreCase));
            if (puzzleIndex >= 0)
            {
                await PostPuzzle(context, puzzleIndex + 1);
            }
        }

        [Command("Clue")]
        [Aliases("Hint")]
        [Description("Get a clue for the current puzzle.")]
        public async Task Clue(CommandContext context)
        {
            int puzzleIndex = NumberWords.FindIndex(word => word.Equals(context.Channel.Name, StringComparison.OrdinalIgnoreCase));
            if (puzzleIndex >= 0)
            {
                await PostClue(context, puzzleIndex + 1);
            }
        }

        [Command("Answer")]
        [Description("Attempt to answer the current puzzle.")]
        public async Task Answer(CommandContext context, string answer)
        {
            int puzzleIndex = NumberWords.FindIndex(word => word.Equals(context.Channel.Name, StringComparison.OrdinalIgnoreCase));
            if (puzzleIndex >= 0)
            {
                string expectedAnswer = GetAnswer(puzzleIndex + 1);
                if (answer.Equals(expectedAnswer, StringComparison.OrdinalIgnoreCase))
                {
                    // Correct.
                    await context.Message.DeleteAsync();
                    await context.RespondAsync(embed: EmbedGenerator.Success($"{context.Member.Mention} got the correct answer."));
                    await AdvanceUserToNextLevel(context, puzzleIndex + 1);
                }
                else
                {
                    await context.RespondAsync(embed: EmbedGenerator.Primary("Wrong"));
                }
            }
        }

        private static async Task AdvanceUserToNextLevel(CommandContext context, int puzzleNumber)
        {
            int nextPuzzleNumber = puzzleNumber + 1;
            DiscordRole currentRole;
            if (puzzleNumber == 1)
            {
                // First question, no previous numbered role.
                currentRole = context.Guild.Roles.Values.FirstOrDefault(role => role.Name.Equals("RabbitHole", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                currentRole = context.Guild.Roles.Values.FirstOrDefault(role => role.Name.Equals($"puzzle-{puzzleNumber.ToWords()}", StringComparison.OrdinalIgnoreCase));
            }
            DiscordRole nextRole = context.Guild.Roles.Values.FirstOrDefault(role => role.Name.Equals($"puzzle-{nextPuzzleNumber.ToWords()}", StringComparison.OrdinalIgnoreCase));

            if (nextRole == default)
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning("Congrats!\nThis is the last question, for now."));
                return;
            }
            await context.Member.GrantRoleAsync(nextRole, "Advanced to this puzzle");
            await context.Member.RevokeRoleAsync(currentRole, "Advanced to the next puzzle");
        }

        private static async Task PostClue(CommandContext context, int puzzleNumber)
        {
            switch (puzzleNumber)
            {
                case 1:
                    await context.RespondAsync(embed: EmbedGenerator.Info("Shakespeare’s Caesar knows the answer every day of the year, but only if you whisper nicely in the correct ear.", "Clue"));
                    break;

                case 2:
                    await context.RespondAsync(embed: EmbedGenerator.Info("Where am I?", "Clue"));
                    break;

                case 3:
                    await context.RespondAsync(embed: EmbedGenerator.Info("Just Sayin'", "Clue"));
                    break;

                case 4:
                    await context.RespondAsync(embed: EmbedGenerator.Info("Yeah, took about 8 hours from heathrow though."));
                    break;

                case 5:
                    await context.RespondAsync(embed: EmbedGenerator.Info("It's not easter yet, is it?"));
                    break;

                case 6:
                    await context.RespondAsync(embed: EmbedGenerator.Info("Do you have dyslexia?"));
                    break;

                case 7:
                    await context.RespondAsync(embed: EmbedGenerator.Info("These fatty acids all have single bonds."));
                    break;

                default:
                    await context.RespondAsync(embed: EmbedGenerator.Info("There is no clue available for this one.", "Good Luck"));
                    break;
            }
        }

        private async Task PostPuzzle(CommandContext context, int puzzleNumber)
        {
            switch (puzzleNumber)
            {
                case 1:
                    await context.RespondAsync(embed: EmbedGenerator.Primary("UifBotxfs"));
                    break;

                case 2:
                    await context.RespondAsync(embed: EmbedGenerator.Primary("0x7F000001"));
                    break;

                case 3:
                    await context.RespondAsync(embed: EmbedGenerator.Primary(".--. .- .-. ... . .. -. - -.--. .----. ..-. # -.-. -.- .----. --..-- / .--. .- .-. ... . .. -. - -.--. .----. .---- ----- ..--.- --- -.. -.. .----. --..-- / -- .- - .... .-.-.- ... --.- .-. - -.--. ..--- ..... -.... -.--.- -.--.- -.--.-"));
                    break;

                case 4:
                    await context.RespondAsync(embed: EmbedGenerator.Primary("That place exists??"));
                    break;

                case 5:
                    await context.RespondAsync(embed: EmbedGenerator.Primary("Copy your name till you can't get further."));
                    break;

                case 6:
                    await context.RespondWithFileAsync(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "Jumbled.jpg"));
                    break;

                case 7:
                    await context.RespondWithFileAsync(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "Hugh.jpg"));
                    break;

                default:
                    await context.RespondAsync(embed: EmbedGenerator.Info("There is no question available for this yet.", "Under Construction"));
                    break;
            }
        }

        private string GetAnswer(int puzzleNumber)
        {
            switch (puzzleNumber)
            {
                case 1:
                    return "TheAnswer";

                case 2:
                    return "Localhost";

                case 3:
                    return "15";

                case 4:
                    return "Timbuktu";

                case 5:
                    return "BEYOND GODLIKE";

                case 6:
                    return "Want";

                case 7:
                    return "Armstrong";

                default:
                    return null;
            }
        }
    }
}