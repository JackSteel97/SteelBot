using DSharpPlus.Entities;
using SteelBot.Channels.Puzzle;
using SteelBot.DiscordModules.Puzzle.Helpers;
using SteelBot.Helpers;
using System;

namespace SteelBot.DiscordModules.Puzzle.Questions.Puzzles;

public class PuzzleTwentyThree : IQuestion
{
    private const int _number = 23;
    private const string _clueText = "There is no extra clue available for this one.";
    private const string _answerText = "Absent Mind";

    /// <inheritdoc />
    public int GetPuzzleNumber() => _number;

    /// <inheritdoc />
    public void PostPuzzle(PuzzleCommandAction request)
    {
        var message = new DiscordMessageBuilder().WithContent(_number.ToString());
        QuestionConstructionHelpers.AddFiles(message, "Lookup.jpg", "OhNo.xlsx");
        request.Responder.Respond(message);
    }

    /// <inheritdoc />
    public void PostClue(PuzzleCommandAction request)
    {
        var message = new DiscordMessageBuilder()
            .WithEmbed(EmbedGenerator.Info(_clueText, "Good Luck"));
        request.Responder.Respond(message);
    }

    /// <inheritdoc />
    public bool AnswerIsCorrect(string answer) => string.Equals(answer, _answerText, StringComparison.OrdinalIgnoreCase);
}