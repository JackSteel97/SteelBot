using DSharpPlus.Entities;
using SteelBot.Channels.Puzzle;
using SteelBot.DiscordModules.Puzzle.Helpers;
using SteelBot.Helpers;
using System;

namespace SteelBot.DiscordModules.Puzzle.Questions.Puzzles;

public class PuzzleThirteen : IQuestion
{
    private const int _number = 13;
    private const string _puzzleFile = "Jumbled.jpg";
    private const string _clueText = "That's so meta.";
    private const string _answerText = "Deep";

    /// <inheritdoc />
    public int GetPuzzleNumber()
    {
        return _number;
    }

    /// <inheritdoc />
    public void PostPuzzle(PuzzleCommandAction request)
    {
        var message = new DiscordMessageBuilder();
        QuestionConstructionHelpers.AddFile(message, _puzzleFile);

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