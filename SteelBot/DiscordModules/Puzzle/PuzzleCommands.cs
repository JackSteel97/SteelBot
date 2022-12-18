using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.Channels.Puzzle;
using SteelBot.DiscordModules.AuditLog.Services;
using SteelBot.Helpers.Extensions;
using SteelBot.Responders;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Puzzle;

[Group("Puzzle")]
[Aliases("Question")]
[RequireGuild]
[Description("Commands for playing the puzzle.")]
public class PuzzleCommands : TypingCommandModule
{
    private const string _puzzleRequirements = "\n\n**You will need:**\nA web browser\n7-Zip\nAn image editing program - e.g. Photoshop / Paint.NET\nAn Audio editing program - e.g. Audacity";
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly CancellationService _cancellationService;
    private readonly PuzzleCommandsChannel _puzzleCommandsChannel;
    private readonly ILogger<PuzzleCommands> _logger;

    public PuzzleCommands(IHub sentry, PuzzleCommandsChannel puzzleCommandsChannel, CancellationService cancellationService, ErrorHandlingService errorHandlingService, ILogger<PuzzleCommands> logger, AuditLogService auditLogService)
        : base(logger, auditLogService, sentry)
    {
        _puzzleCommandsChannel = puzzleCommandsChannel;
        _cancellationService = cancellationService;
        _errorHandlingService = errorHandlingService;
        _logger = logger;
    }

    [GroupCommand]
    [Description("Get the current puzzle." + _puzzleRequirements)]
    [Cooldown(5, 60, CooldownBucketType.Channel)]
    public async Task Puzzle(CommandContext context)
    {
        _logger.LogInformation("User {UserId} requested to view the puzzle", context.Member.Id);
        var message = new PuzzleCommandAction(PuzzleCommandActionType.Puzzle, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild);
        await _puzzleCommandsChannel.Write(message, _cancellationService.Token);
    }

    [Command("Clue")]
    [Aliases("Hint")]
    [Description("Get a clue for the current puzzle." + _puzzleRequirements)]
    [Cooldown(5, 60, CooldownBucketType.Channel)]
    public async Task Clue(CommandContext context)
    {
        _logger.LogInformation("User {UserId} requested to view the puzzle clue", context.Member.Id);
        var message = new PuzzleCommandAction(PuzzleCommandActionType.Clue, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild);
        await _puzzleCommandsChannel.Write(message, _cancellationService.Token);
    }

    [Command("Answer")]
    [Description("Attempt to answer the current puzzle." + _puzzleRequirements)]
    [Cooldown(10, 60, CooldownBucketType.User)]
    public async Task Answer(CommandContext context, [RemainingText] string answer)
    {
        _logger.LogInformation("User {UserId} submitted '{Answer}' as an answer to the puzzle", context.Member.Id, answer);
        var message = new PuzzleCommandAction(PuzzleCommandActionType.Answer, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild, answer);
        await _puzzleCommandsChannel.Write(message, _cancellationService.Token);
    }
}