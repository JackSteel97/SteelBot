﻿using DSharpPlus.Entities;
using SteelBot.Channels.Puzzle;
using SteelBot.Helpers;
using System;

namespace SteelBot.DiscordModules.Puzzle.Questions.Puzzles;

public class PuzzleFourteen : IQuestion
{
    private const int _number = 14;
    private const string _puzzleText = "perfumed deferring hotspots";
    private const string _clueText = "These 3 words";
    private const string _answerText = "Angkor Wat";

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