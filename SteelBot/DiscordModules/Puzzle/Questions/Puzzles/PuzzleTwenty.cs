using DSharpPlus.Entities;
using SteelBot.Channels.Puzzle;
using SteelBot.DiscordModules.Puzzle.Helpers;
using SteelBot.Helpers;
using System;

namespace SteelBot.DiscordModules.Puzzle.Questions.Puzzles;

public class PuzzleTwenty : IQuestion
{
    private const int _number = 20;
    private const string _puzzleFile = "West.jpg";
    private const string _clueText = "Got a waterproof map?";
    private const string _answerText = "Sea Caves";

    /// <inheritdoc />
    public int GetPuzzleNumber()
    {
        return _number;
    }

    /// <inheritdoc />
    public void PostPuzzle(PuzzleCommandAction request)
    {
        var message = new DiscordMessageBuilder().WithContent(_number.ToString());
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