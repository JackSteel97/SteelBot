using DSharpPlus.Entities;
using SteelBot.Channels.Puzzle;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DiscordModules.Puzzle.Questions;
using SteelBot.Helpers;
using System;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Puzzle.Services;

public class PuzzleService
{
    private readonly PuzzleProvider _puzzleProvider;
    private readonly QuestionFactory _questionFactory;

    public PuzzleService(PuzzleProvider puzzleProvider, QuestionFactory questionFactory)
    {
        _puzzleProvider = puzzleProvider;
        _questionFactory = questionFactory; 
    }

    public async Task Question(PuzzleCommandAction request)
    {
        if (request.Action != PuzzleCommandActionType.Puzzle) throw new ArgumentException($"Unexpected action type sent to {nameof(Question)}");

        var question = await GetCurrentQuestion(request.Member);
        question.PostPuzzle(request);
    }

    public async Task Clue(PuzzleCommandAction request)
    {
        if (request.Action != PuzzleCommandActionType.Clue) throw new ArgumentException($"Unexpected action type sent to {nameof(Question)}");

        var question = await GetCurrentQuestion(request.Member);
        question.PostClue(request);
    }
    
    public async Task Answer(PuzzleCommandAction request)
    {
        if (request.Action != PuzzleCommandActionType.Answer) throw new ArgumentException($"Unexpected action type sent to {nameof(Question)}");

        var question = await GetCurrentQuestion(request.Member);
        await _puzzleProvider.RecordGuess(request.Member.Id, question.GetPuzzleNumber(), request.GivenAnswer);
        
        if (question.AnswerIsCorrect(request.GivenAnswer))
        {
            request.Responder.Respond(new DiscordMessageBuilder().WithEmbed(EmbedGenerator.Success($"{request.Member.Mention} got the correct answer.")), ephemeral: true);
            await _puzzleProvider.SetUserPuzzleLevel(request.Member.Id, question.GetPuzzleNumber() + 1);
        }
        else
        {
            request.Responder.Respond(new DiscordMessageBuilder().WithEmbed(EmbedGenerator.Info("Incorrect")));
        }
    }

    private async Task<IQuestion> GetCurrentQuestion(DiscordMember member)
    {
        int usersPuzzleLevel = await _puzzleProvider.GetUserPuzzleLevel(member.Id);
        return _questionFactory.GetQuestion(usersPuzzleLevel);
    }
}