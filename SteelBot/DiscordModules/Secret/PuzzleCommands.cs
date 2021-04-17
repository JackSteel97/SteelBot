using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Humanizer;
using SteelBot.Attributes;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Secret
{
    [Group("Puzzle")]
    [Aliases("Question")]
    [RequireGuild]
    [GuildCheck(287309906137055247, 782237087352356876)]
    [Description("Commands for playing the puzzle. These commands only work in the puzzle channels.")]
    public class PuzzleCommands : TypingCommandModule
    {
        private const string PuzzleRequirements = "\n\n**You will need:**\nA web browser\n7-Zip\nAn image editing program - e.g. Photoshop / Paint.NET\nAn Audio editing program - e.g. Audacity\n\nIf you find any problems DM Jack.";
        private const int NumberOfQuestions = 29;
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

        [GroupCommand]
        [Description("Get the current puzzle." + PuzzleRequirements)]
        [Cooldown(5, 60, CooldownBucketType.Channel)]
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
        [Description("Get a clue for the current puzzle." + PuzzleRequirements)]
        [Cooldown(5, 60, CooldownBucketType.Channel)]
        public async Task Clue(CommandContext context)
        {
            int puzzleIndex = NumberWords.FindIndex(word => word.Equals(context.Channel.Name, StringComparison.OrdinalIgnoreCase));
            if (puzzleIndex >= 0)
            {
                await PostClue(context, puzzleIndex + 1);
            }
        }

        [Command("Answer")]
        [Description("Attempt to answer the current puzzle." + PuzzleRequirements)]
        [Cooldown(10, 60, CooldownBucketType.User)]
        public async Task Answer(CommandContext context, [RemainingText] string answer)
        {
            int puzzleIndex = NumberWords.FindIndex(word => word.Equals(context.Channel.Name, StringComparison.OrdinalIgnoreCase));
            if (puzzleIndex >= 0)
            {
                string expectedAnswer = GetAnswer(puzzleIndex + 1);
                await context.Message.DeleteAsync();

                if (answer.Equals(expectedAnswer, StringComparison.OrdinalIgnoreCase))
                {
                    // Correct.
                    await context.RespondAsync(embed: EmbedGenerator.Success($"{context.Member.Mention} got the correct answer."));
                    await AdvanceUserToNextLevel(context, puzzleIndex + 1);
                }
                else
                {
                    await context.RespondAsync(embed: EmbedGenerator.Primary($"{context.User.Mention} Incorrect"));

                    // Send audit message to me.
                    ulong civlationId = AppConfigurationService.Application.CommonServerId;
                    ulong jackId = AppConfigurationService.Application.CreatorUserId;

                    DiscordGuild commonServer = await context.Client.GetGuildAsync(civlationId);
                    DiscordMember jack = await commonServer.GetMemberAsync(jackId);

                    await jack.SendMessageAsync(embed: EmbedGenerator.Info($"**{context.Member.Nickname ?? context.Member.Username}** submitted an incorrect answer [**{answer}**] to puzzle [**{(puzzleIndex + 1).ToWords()}**]", "Puzzle Incorrect Submission"));
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
            DiscordMessageBuilder message = new DiscordMessageBuilder();

            switch (puzzleNumber)
            {
                case 1:
                    message.WithEmbed(EmbedGenerator.Info("Shakespeare’s Caesar knows the answer every day of the year, but only if you whisper nicely in the correct ear.", "Clue"));
                    break;

                case 2:
                    message.WithEmbed(EmbedGenerator.Info("Where am I?", "Clue"));
                    break;

                case 3:
                    message.WithEmbed(EmbedGenerator.Info("Just Sayin'", "Clue"));
                    break;

                case 4:
                    message.WithEmbed(EmbedGenerator.Info("Oui, took about 8 hours from Heathrow. There's actually a whole region of the same name, they're even on GMT down there too.", "Clue"));
                    break;

                case 5:
                    message.WithEmbed(EmbedGenerator.Info("It's not easter yet, is it?", "Clue"));
                    break;

                case 6:
                    message.WithEmbed(EmbedGenerator.Info("Do you have dyslexia?", "Clue"));
                    break;

                case 7:
                    message.WithEmbed(EmbedGenerator.Info("These fatty acids all have single bonds.", "Clue"));
                    break;

                case 8:
                    message.WithEmbed(EmbedGenerator.Info("Into the waves.", "Clue"));
                    break;

                case 9:
                    message.WithEmbed(EmbedGenerator.Info("The perfect spy.", "Clue"));
                    break;

                case 10:
                    message.WithEmbed(EmbedGenerator.Info($"I promise I'm not trying to Console.Scam() you {DiscordEmoji.FromName(context.Client, ":innocent:")}", "Clue"));
                    break;

                case 11:
                    message.WithEmbed(EmbedGenerator.Info("OwO.", "Clue"));
                    break;

                case 12:
                    message.WithEmbed(EmbedGenerator.Info("mmmmmmmmmmm.", "Clue"));
                    break;

                case 13:
                    message.WithEmbed(EmbedGenerator.Info("That's so meta.", "Clue"));
                    break;

                case 14:
                    message.WithEmbed(EmbedGenerator.Info("These 3 words", "Clue"));
                    break;

                case 15:
                    message.WithEmbed(EmbedGenerator.Info("All zipped up for the waves", "Clue"));
                    break;

                case 16:
                    message.WithEmbed(EmbedGenerator.Info("Hash brown", "Clue"));
                    break;

                case 17:
                    message.WithEmbed(EmbedGenerator.Info("We start from zero in this house, Library nerd", "Clue"));
                    break;

                case 18:
                    message.WithEmbed(EmbedGenerator.Info("Where did we first meet?", "Clue"));
                    break;

                case 19:
                    message.WithEmbed(EmbedGenerator.Info("Name it.", "Clue"));
                    break;

                case 20:
                    message.WithEmbed(EmbedGenerator.Info("Got a waterproof map?", "Clue"));
                    break;

                case 21:
                    message.WithEmbed(EmbedGenerator.Info("Purple night message received, sir!", "Clue"));
                    break;

                case 22:
                    message.WithEmbed(EmbedGenerator.Info("We need more dimensions!", "Clue"));
                    break;

                case 24:
                    message.WithEmbed(EmbedGenerator.Info("Answer the question.", "Clue"));
                    break;

                case 25:
                    message.WithEmbed(EmbedGenerator.Info("You already have everything you need.", "Clue"));
                    break;

                case 28:
                    message.WithEmbed(EmbedGenerator.Info("Give your answer as words.", "Clue", "Use dashes to separate words (same way as the level names)."));
                    break;

                case 29:
                    message.WithEmbed(EmbedGenerator.Info("`2020-11-22` - `2020-12-12`", "Clue", "Give your answer in words."));
                    break;

                default:
                    message.WithEmbed(EmbedGenerator.Info("There is no extra clue available for this one.", "Good Luck"));
                    break;
            }
            await context.RespondAsync(message);
        }

        private async Task PostPuzzle(CommandContext context, int puzzleNumber)
        {
            DiscordMessageBuilder message = new DiscordMessageBuilder();
            FileStream reader = null;
            Dictionary<string, Stream> fileStreams = new Dictionary<string, Stream>();
            switch (puzzleNumber)
            {
                case 1:
                    message.WithEmbed(EmbedGenerator.Primary("UifBotxfs"));
                    break;

                case 2:
                    message.WithEmbed(EmbedGenerator.Primary("0x7F000001"));
                    break;

                case 3:
                    message.WithEmbed(EmbedGenerator.Primary(".--. .- .-. ... . .. -. - -.--. .----. ..-. # -.-. -.- .----. --..-- / .--. .- .-. ... . .. -. - -.--. .----. .---- ----- ..--.- --- -.. -.. .----. --..-- / -- .- - .... .-.-.- ... --.- .-. - -.--. ..--- ..... -.... -.--.- -.--.- -.--.-"));
                    break;

                case 4:
                    message.WithEmbed(EmbedGenerator.Primary("That place exists, I thought it was made up to mean far-away??"));
                    break;

                case 5:

                    message.WithEmbed(EmbedGenerator.Primary("Copy your name till you can't get further."));
                    break;

                case 6:
                    reader = AddFile(message, "Jumbled.jpg");
                    break;

                case 7:
                    reader = AddFile(message, "Hugh.jpg");
                    break;

                case 8:
                    reader = AddFile(message, "Outside.wav");
                    break;

                case 9:
                    reader = AddFile(message, "Double.mp3");
                    break;

                case 10:
                    message.WithEmbed(EmbedGenerator.Primary("control I"));
                    break;

                case 11:
                    message.WithEmbed(EmbedGenerator.Primary("Notice me Senpai"));
                    break;

                case 12:
                    FileStream imageStream = File.OpenRead(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "Cow.jpg"));
                    FileStream zipStream = File.OpenRead(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "Inside.zip"));
                    fileStreams.Add("Cow.jpg", imageStream);
                    fileStreams.Add("Inside.zip", zipStream);
                    message.WithFiles(fileStreams).WithEmbed(EmbedGenerator.Primary("It's what's on the inside that counts."));
                    break;

                case 13:
                    reader = AddFile(message, "Jumbled.jpg");
                    break;

                case 14:
                    message.WithEmbed(EmbedGenerator.Primary("perfumed deferring hotspots"));
                    break;

                case 15:
                    reader = AddFile(message, "Seven.jpg");
                    break;

                case 16:
                    reader = AddFile(message, "ATastyTreat.txt");
                    break;

                case 17:
                    reader = AddFile(message, "ThatDoesNotGoThere.png");
                    break;

                case 18:
                    reader = AddFile(message, "Goodbye.png");
                    break;

                case 19:
                    reader = AddFile(message, "PrintMe.png");
                    break;

                case 20:
                    reader = AddFile(message, "West.jpg");
                    break;

                case 21:
                    reader = AddFile(message, "Vietnam.png");
                    break;

                case 22:
                    reader = AddFile(message, "Trippy.jpg");
                    break;

                case 23:
                    FileStream imageStream23 = File.OpenRead(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "LookUp.jpg"));
                    FileStream excelStream23 = File.OpenRead(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "OhNo.xlsx"));
                    fileStreams.Add("LookUp.jpg", imageStream23);
                    fileStreams.Add("OhNo.xlsx", excelStream23);
                    message.WithFiles(fileStreams);
                    break;

                case 24:
                    DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder().WithColor(EmbedGenerator.InfoColour)
                        .AddField("First", "Bell Number Four")
                        .AddField("Second", "Lucas Number Six")
                        .AddField("Third", "Motzkin Number Five")
                        .AddField("Fourth", "Catalan Number Two")
                        .AddField("Fifth", "Fibonacci Number Six");

                    reader = AddFile(message, "DidYouTakeNotes.zip");
                    message.WithEmbed(embedBuilder.Build());
                    break;

                case 25:
                    reader = AddFile(message, "ItsAllThere.mp3");
                    break;

                case 26:
                    reader = AddFile(message, "Spook.wav");
                    break;

                case 27:
                    reader = AddFile(message, "ISO8601.mp3");
                    break;

                case 28:
                    reader = AddFile(message, "25.jpg");
                    break;

                case 29:
                    reader = AddFile(message, "Ooer.png");
                    break;

                default:
                    message.WithEmbed(EmbedGenerator.Info("There is no question available for this yet.", "Under Construction"));
                    break;
            }
            await context.RespondAsync(message);

            // Dispose streams.
            if (reader != null)
            {
                reader.Dispose();
            }
            foreach (Stream stream in fileStreams.Values)
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }

        private static string GetAnswer(int puzzleNumber)
        {
            return puzzleNumber switch
            {
                1 => "TheAnswer",
                2 => "Localhost",
                3 => "15",
                4 => "Timbuktu",
                5 => "BEYOND GODLIKE",
                6 => "Want",
                7 => "Armstrong",
                8 => "Eggs",
                9 => "Bond",
                10 => "Hold Up!",
                11 => "uWu",
                12 => "Yummy",
                13 => "Deep",
                14 => "Angkor Wat",
                15 => "Apples",
                16 => "unsecure",
                17 => "Ender's Game",
                18 => "Newcastle",
                19 => "Drab",
                20 => "Sea Caves",
                21 => "Kerplunk",
                22 => "SELF DESTRUCT",
                23 => "Absent Mind",
                24 => "Ankle Biters",
                25 => "Electric Eel Embrace",
                26 => "Boop",
                27 => DateTime.UtcNow.ToString("HH:mm"),
                28 => "Eight",
                29 => "Two",
                _ => null,
            };
        }

        private FileStream AddFile(DiscordMessageBuilder message, string fileName)
        {
            var fs = new FileStream(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", fileName), FileMode.Open, FileAccess.Read);
            message.WithFile(fileName, fs);
            return fs;
        }
    }
}