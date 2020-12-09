using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using SteelBot.Attributes;
using SteelBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using DSharpPlus.Entities;
using SteelBot.Services.Configuration;
using System.IO;

namespace SteelBot.DiscordModules.Secret
{
    [Group("Puzzle")]
    [Aliases("Question")]
    [RequireGuild]
    [GuildCheck(287309906137055247, 782237087352356876)]
    [Description("Commands for playing the puzzle. These commands only work in the puzzle channels.")]
    public class PuzzleCommands : BaseCommandModule
    {
        private const string PuzzleRequirements = "\n\n**You will need:**\nA web browser\n7-Zip\nAn image editing program - e.g. Photoshop / Paint.NET\nAn Audio editing program - e.g. Audacity\n\nIf you find any problems DM Jack.";
        private const int NumberOfQuestions = 28;
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
                    await context.RespondAsync(embed: EmbedGenerator.Info("Oui, took about 8 hours from Heathrow. There's actually a whole region of the same name, they're even on GMT down there too.", "Clue"));
                    break;

                case 5:
                    await context.RespondAsync(embed: EmbedGenerator.Info("It's not easter yet, is it?", "Clue"));
                    break;

                case 6:
                    await context.RespondAsync(embed: EmbedGenerator.Info("Do you have dyslexia?", "Clue"));
                    break;

                case 7:
                    await context.RespondAsync(embed: EmbedGenerator.Info("These fatty acids all have single bonds.", "Clue"));
                    break;

                case 8:
                    await context.RespondAsync(embed: EmbedGenerator.Info("Into the waves.", "Clue"));
                    break;

                case 9:
                    await context.RespondAsync(embed: EmbedGenerator.Info("The perfect spy.", "Clue"));
                    break;

                case 10:
                    await context.RespondAsync(embed: EmbedGenerator.Info($"I promise I'm not trying to Console.Scam() you {DiscordEmoji.FromName(context.Client, ":innocent:")}", "Clue"));
                    break;

                case 11:
                    await context.RespondAsync(embed: EmbedGenerator.Info("OwO.", "Clue"));
                    break;

                case 12:
                    await context.RespondAsync(embed: EmbedGenerator.Info("mmmmmmmmmmm.", "Clue"));
                    break;

                case 13:
                    await context.RespondAsync(embed: EmbedGenerator.Info("That's so meta.", "Clue"));
                    break;

                case 14:
                    await context.RespondAsync(embed: EmbedGenerator.Info("These 3 words", "Clue"));
                    break;

                case 15:
                    await context.RespondAsync(embed: EmbedGenerator.Info("All zipped up for the waves", "Clue"));
                    break;

                case 16:
                    await context.RespondAsync(embed: EmbedGenerator.Info("Hash brown", "Clue"));
                    break;

                case 17:
                    await context.RespondAsync(embed: EmbedGenerator.Info("We start from zero in this house, Library nerd", "Clue"));
                    break;

                case 18:
                    await context.RespondAsync(embed: EmbedGenerator.Info("Where did we first meet?", "Clue"));
                    break;

                case 19:
                    await context.RespondAsync(embed: EmbedGenerator.Info("Name it.", "Clue"));
                    break;

                case 20:
                    await context.RespondAsync(embed: EmbedGenerator.Info("Got a waterproof map?", "Clue"));
                    break;

                case 21:
                    await context.RespondAsync(embed: EmbedGenerator.Info("Purple night message received, sir!", "Clue"));
                    break;

                case 22:
                    await context.RespondAsync(embed: EmbedGenerator.Info("We need more dimensions!", "Clue"));
                    break;

                case 24:
                    await context.RespondAsync(embed: EmbedGenerator.Info("Answer the question.", "Clue"));
                    break;

                case 25:
                    await context.RespondAsync(embed: EmbedGenerator.Info("You already have everything you need.", "Clue"));
                    break;

                case 28:
                    await context.RespondAsync(embed: EmbedGenerator.Info("Give your answer as words.", "Clue", "Use dashes to separate words (same way as the level names)."));
                    break;

                default:
                    await context.RespondAsync(embed: EmbedGenerator.Info("There is no extra clue available for this one.", "Good Luck"));
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
                    await context.RespondAsync(embed: EmbedGenerator.Primary("That place exists, I thought it was made up to mean far-away??"));
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

                case 8:
                    await context.RespondWithFileAsync(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "Outside.wav"));
                    break;

                case 9:
                    await context.RespondWithFileAsync(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "Double.mp3"));
                    break;

                case 10:
                    await context.RespondAsync(embed: EmbedGenerator.Primary("control I"));
                    break;

                case 11:
                    await context.RespondAsync(embed: EmbedGenerator.Primary("Notice me Senpai"));
                    break;

                case 12:
                    FileStream imageStream = File.OpenRead(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "Cow.jpg"));
                    FileStream zipStream = File.OpenRead(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "Inside.zip"));
                    Dictionary<string, Stream> streams = new Dictionary<string, Stream>
                    {
                        { "Cow.jpg", imageStream },
                        { "Inside.zip", zipStream }
                    };
                    await context.RespondWithFilesAsync(streams, embed: EmbedGenerator.Primary("It's what's on the inside that counts."));
                    break;

                case 13:
                    await context.RespondWithFileAsync(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "Inception.jpg"));
                    break;

                case 14:
                    await context.RespondAsync(embed: EmbedGenerator.Primary("perfumed deferring hotspots"));
                    break;

                case 15:
                    await context.RespondWithFileAsync(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "Seven.jpg"));
                    break;

                case 16:
                    await context.RespondWithFileAsync(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "ATastyTreat.txt"));
                    break;

                case 17:
                    await context.RespondWithFileAsync(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "ThatDoesNotGoThere.png"));
                    break;

                case 18:
                    await context.RespondWithFileAsync(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "Goodbye.png"));
                    break;

                case 19:
                    await context.RespondWithFileAsync(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "PrintMe.png"));
                    break;

                case 20:
                    await context.RespondWithFileAsync(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "West.jpg"));
                    break;

                case 21:
                    await context.RespondWithFileAsync(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "Vietnam.png"));
                    break;

                case 22:
                    await context.RespondWithFileAsync(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "Trippy.jpg"));
                    break;

                case 23:
                    FileStream imageStream23 = File.OpenRead(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "LookUp.jpg"));
                    FileStream excelStream23 = File.OpenRead(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "OhNo.xlsx"));
                    Dictionary<string, Stream> streams23 = new Dictionary<string, Stream>
                    {
                        { "LookUp.jpg", imageStream23 },
                        { "OhNo.xlsx", excelStream23 }
                    };
                    await context.RespondWithFilesAsync(streams23);
                    break;

                case 24:
                    DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder().WithColor(EmbedGenerator.InfoColour)
                        .AddField("First", "Bell Number Four")
                        .AddField("Second", "Lucas Number Six")
                        .AddField("Third", "Motzkin Number Five")
                        .AddField("Fourth", "Catalan Number Two")
                        .AddField("Fifth", "Fibonacci Number Six");
                    await context.RespondWithFileAsync(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "DidYouTakeNotes.zip"), embed: embedBuilder.Build());
                    break;

                case 25:
                    await context.RespondWithFileAsync(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "ItsAllThere.mp3"));
                    break;

                case 26:
                    await context.RespondWithFileAsync(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "Spook.wav"));
                    break;

                case 27:
                    await context.RespondWithFileAsync(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "ISO8601.mp3"));
                    break;

                case 28:
                    await context.RespondWithFileAsync(Path.Combine(AppConfigurationService.BasePath, "Resources", "Puzzle", "25.jpg"));
                    break;

                default:
                    await context.RespondAsync(embed: EmbedGenerator.Info("There is no question available for this yet.", "Under Construction"));
                    break;
            }
        }

        private static string GetAnswer(int puzzleNumber)
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

                case 8:
                    return "Eggs";

                case 9:
                    return "Bond";

                case 10:
                    return "Hold Up!";

                case 11:
                    return "uWu";

                case 12:
                    return "Yummy";

                case 13:
                    return "Deep";

                case 14:
                    return "Angkor Wat";

                case 15:
                    return "Apples";

                case 16:
                    return "unsecure";

                case 17:
                    return "Ender's Game";

                case 18:
                    return "Newcastle";

                case 19:
                    return "Drab";

                case 20:
                    return "Sea Caves";

                case 21:
                    return "Kerplunk";

                case 22:
                    return "SELF DESTRUCT";

                case 23:
                    return "Absent Mind";

                case 24:
                    return "Ankle Biters";

                case 25:
                    return "Electric Eel Embrace";

                case 26:
                    return "Boop";

                case 27:
                    return DateTime.UtcNow.ToString("HH:mm");

                case 28:
                    return "Eight";

                default:
                    return null;
            }
        }
    }
}