using DSharpPlus.Entities;
using SteelBot.Channels.Puzzle;
using SteelBot.Helpers;
using System;

namespace SteelBot.DiscordModules.Puzzle.Questions.Puzzles;

public class PuzzleTwo : IQuestion
{
    private const int _number = 2;
    private const string _puzzleText = "0x7F000001";
    private const string _clueText = "Where am I?";
    private const string _answerText = "Localhost";

    /// <inheritdoc />
    public int GetPuzzleNumber()
    {
        return _number;
    }

    /// <inheritdoc />
    public void PostPuzzle(PuzzleCommandAction request)
    {
        var message = new DiscordMessageBuilder()
            .WithEmbed(EmbedGenerator.Primary(_puzzleText));
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
    public bool AnswerIsCorrect(string answer)
    {
        return string.Equals(answer, _answerText, StringComparison.OrdinalIgnoreCase);
    }
}