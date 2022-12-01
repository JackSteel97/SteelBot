using DSharpPlus.Entities;
using SteelBot.Channels.Puzzle;
using SteelBot.Helpers;
using System;

namespace SteelBot.DiscordModules.Puzzle.Questions.Puzzles;

public class PuzzleThree : IQuestion
{
    private const int _number = 3;
    private const string _puzzleText = ".--. .- .-. ... . .. -. - -.--. .----. ..-. # -.-. -.- .----. --..-- / .--. .- .-. ... . .. -. - -.--. .----. .---- ----- ..--.- --- -.. -.. .----. --..-- / -- .- - .... .-.-.- ... --.- .-. - -.--. ..--- ..... -.... -.--.- -.--.- -.--.-";
    private const string _clueText = "Just Sayin'";
    private const string _answerText = "15";

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