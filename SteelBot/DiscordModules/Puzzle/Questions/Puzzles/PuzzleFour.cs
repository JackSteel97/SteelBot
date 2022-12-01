using DSharpPlus.Entities;
using SteelBot.Channels.Puzzle;
using SteelBot.Helpers;
using System;

namespace SteelBot.DiscordModules.Puzzle.Questions.Puzzles;

public class PuzzleFour : IQuestion
{
    private const int _number = 4;
    private const string _puzzleText = "That place exists, I thought it was made up to mean far-away??";
    private const string _clueText = "Oui, took about 8 hours from Heathrow. There's actually a whole region of the same name, they're even on GMT down there too.";
    private const string _answerText = "Timbuktu";

    /// <inheritdoc />
    public int GetPuzzleNumber()
    {
        return _number;
    }

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
    public bool AnswerIsCorrect(string answer)
    {
        return string.Equals(answer, _answerText, StringComparison.OrdinalIgnoreCase);
    }
}