using DSharpPlus.Entities;
using SteelBot.Channels.Puzzle;
using SteelBot.Helpers;
using System;

namespace SteelBot.DiscordModules.Puzzle.Questions.Puzzles;

public class PuzzleTen : IQuestion
{
    private const int _number = 10;
    private const string _puzzleText = "control I";
    private const string _clueText = "I promise I'm not trying to Console.Scam() you...";
    private const string _answerText = "Hold Up!";

    /// <inheritdoc />
    public int GetPuzzleNumber() => _number;

    /// <inheritdoc />
    public void PostPuzzle(PuzzleCommandAction request)
    {
        var message = new DiscordMessageBuilder()
            .WithEmbed(EmbedGenerator.Primary(_puzzleText, _number.ToString()));

        request.Responder.Respond(message);
    }

    /// <inheritdoc />
    public void PostClue(PuzzleCommandAction request)
    {
        var message = new DiscordMessageBuilder()
            .WithEmbed(EmbedGenerator.Info(_clueText, "Clue"));
        request.Responder.Respond(message);
    }

    /// <inheritdoc />
    public bool AnswerIsCorrect(string answer) => string.Equals(answer, _answerText, StringComparison.OrdinalIgnoreCase);
}