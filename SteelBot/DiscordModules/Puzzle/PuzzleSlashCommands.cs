using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.Logging;
using SteelBot.Channels.Puzzle;
using SteelBot.DiscordModules.AuditLog.Services;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Responders;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Puzzle;

[SlashCommandGroup("Puzzle", "Commands for playing the puzzle")]
[SlashRequireGuild]
public class PuzzleSlashCommands : InstrumentedApplicationCommandModule
{
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly CancellationService _cancellationService;
    private readonly PuzzleCommandsChannel _puzzleCommandsChannel;
    private readonly ILogger<PuzzleSlashCommands> _logger;

    /// <inheritdoc />
    public PuzzleSlashCommands(ErrorHandlingService errorHandlingService, CancellationService cancellationService, PuzzleCommandsChannel puzzleCommandsChannel, ILogger<PuzzleSlashCommands> logger, AuditLogService auditLogService) : base(logger, auditLogService)
    {
        _errorHandlingService = errorHandlingService;
        _cancellationService = cancellationService;
        _puzzleCommandsChannel = puzzleCommandsChannel;
        _logger = logger;  
    }

    [SlashCommand("Question", "Get the current puzzle question")]
    [SlashCooldown(5, 60, SlashCooldownBucketType.User)]
    public async Task Puzzle(InteractionContext context)
    {
        _logger.LogInformation("[Slash Command] User {UserId} requested to view the puzzle", context.User.Id);
        var message = new PuzzleCommandAction(PuzzleCommandActionType.Puzzle, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild);
        await _puzzleCommandsChannel.Write(message, _cancellationService.Token);
    }

    [SlashCommand("Clue", "Get a clue for the current puzzle")]
    [SlashCooldown(5, 60, SlashCooldownBucketType.User)]
    public async Task Clue(InteractionContext context)
    {
        _logger.LogInformation("[Slash Command] User {UserId} requested to view the puzzle clue", context.Member.Id);
        var message = new PuzzleCommandAction(PuzzleCommandActionType.Clue, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild);
        await _puzzleCommandsChannel.Write(message, _cancellationService.Token);
    }

    [SlashCommand("Answer", "Attempt to answer the current puzzle")]
    [SlashCooldown(10, 60, SlashCooldownBucketType.User)]
    public async Task Answer(InteractionContext context, [Option("Answer", "Your answer for the current question")] string answer)
    {
        _logger.LogInformation("[Slash Command] User {UserId} submitted '{Answer}' as an answer to the puzzle", context.Member.Id, answer);
        var message = new PuzzleCommandAction(PuzzleCommandActionType.Answer, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild, answer);
        await _puzzleCommandsChannel.Write(message, _cancellationService.Token);
    }

    [SlashCommand("Requirements", "View the requirements for the entire puzzle")]
    [SlashCooldown(5, 60, SlashCooldownBucketType.User)]
    public Task Requirements(InteractionContext context)
    {
        _logger.LogInformation("[Slash Command] User {UserId} requested to view the puzzle requirements", context.User.Id);
        var responder = new InteractionResponder(context, _errorHandlingService);
        var message = new DiscordMessageBuilder().WithEmbed(
            EmbedGenerator.Info("**You will need:**\nA web browser\n7-Zip\nAn image editing program - e.g. Photoshop / Paint.NET\nAn Audio editing program - e.g. Audacity", "Requirements for the entire puzzle"));
        responder.Respond(message);
        return Task.CompletedTask;
    }
}