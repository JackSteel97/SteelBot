using DSharpPlus.Entities;
using SteelBot.Channels.Puzzle;
using SteelBot.Helpers;
using System;

namespace SteelBot.DiscordModules.Puzzle.Questions.Puzzles;

public class PuzzleFive : IQuestion
{
    private const int _number = 5;
    private const string _puzzleText = "Copy your name till you can't get further.";
    private const string _clueText = "It's not easter yet, is it?";
    private const string _answerText = "BEYOND GODLIKE";

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