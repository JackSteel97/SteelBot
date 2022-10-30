namespace SteelBot.Database.Models.Puzzle;

public class Guess
{
    public int Id { get; set; }
    public ulong UserId { get; set; }
    public int PuzzleLevel { get; set; }
    public string GuessContent { get; set; }
}