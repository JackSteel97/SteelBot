using DSharpPlus.Entities;
using SteelBot.Channels.Puzzle;
using SteelBot.DiscordModules.Puzzle.Helpers;
using SteelBot.Helpers;
using System;

namespace SteelBot.DiscordModules.Puzzle.Questions.Puzzles;

public class PuzzleTwentySix : IQuestion
{
    private const int _number = 26;
    private const string _puzzleFile = "Spook.wav";
    private const string _clueText = "There is no extra clue available for this one.";
    private const string _answerText = "Boop";

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
            .WithEmbed(EmbedGenerator.Info(_clueText, "Good Luck"));
        request.Responder.Respond(message);
    }

    /// <inheritdoc />
    public bool AnswerIsCorrect(string answer)
    {
        return string.Equals(answer, _answerText, StringComparison.OrdinalIgnoreCase);
    }
}