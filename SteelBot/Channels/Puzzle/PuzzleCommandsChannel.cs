using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.DiscordModules.Puzzle.Services;
using SteelBot.Helpers.Sentry;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.Channels.Puzzle;

public class PuzzleCommandsChannel : BaseChannel<PuzzleCommandAction>
{
    private readonly PuzzleService _puzzleService;
    private readonly IHub _sentry;

    /// <inheritdoc />
    public PuzzleCommandsChannel(PuzzleService puzzleService, IHub sentry, ILogger<PuzzleCommandsChannel> logger, ErrorHandlingService errorHandlingService, string channelLabel = "Puzzle") : base(logger, errorHandlingService, channelLabel)
    {
        _puzzleService = puzzleService;
        _sentry = sentry;   
    }

    /// <inheritdoc />
    protected override async ValueTask HandleMessage(PuzzleCommandAction message)
    {
        var transaction = _sentry.StartNewConfiguredTransaction("Puzzle", message.Action.ToString(), message.Member, message.Guild);

        switch (message.Action)
        {
            case PuzzleCommandActionType.Puzzle:
                await _puzzleService.Question(message);
                break;
            case PuzzleCommandActionType.Clue:
                await _puzzleService.Clue(message);
                break;
            case PuzzleCommandActionType.Answer:
                await _puzzleService.Answer(message);
                break;
        }
        transaction.Finish();
    }
}