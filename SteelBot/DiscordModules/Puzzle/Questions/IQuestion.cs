using SteelBot.Channels.Puzzle;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Puzzle.Questions;

public interface IQuestion
{
    int GetPuzzleNumber();
    void PostPuzzle(PuzzleCommandAction request);
    void PostClue(PuzzleCommandAction request);
    bool AnswerIsCorrect(string answer);
}