using DSharpPlus.Entities;
using SteelBot.Channels.Puzzle;
using SteelBot.DiscordModules.Puzzle.Helpers;
using SteelBot.Helpers;
using System;

namespace SteelBot.DiscordModules.Puzzle.Questions.Puzzles;

public class PuzzleTwentyFour : IQuestion
{
    private const int _number = 24;
    private const string _puzzleFile = "DidYouTakeNotes.zip";
    private const string _clueText = "Answer the question.";
    private const string _answerText = "Ankle Biters";

    /// <inheritdoc />
    public int GetPuzzleNumber() => _number;

    /// <inheritdoc />
    public void PostPuzzle(PuzzleCommandAction request)
    {
        var embedBuilder = new DiscordEmbedBuilder().WithColor(EmbedGenerator.InfoColour).WithTitle(_number.ToString())
            .AddField("First", "Bell Number Four")
            .AddField("Second", "Lucas Number Six")
            .AddField("Third", "Motzkin Number Five")
            .AddField("Fourth", "Catalan Number Two")
            .AddField("Fifth", "Fibonacci Number Six");
        var message = new DiscordMessageBuilder().WithEmbed(embedBuilder);
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
    public bool AnswerIsCorrect(string answer) => string.Equals(answer, _answerText, StringComparison.OrdinalIgnoreCase);
}