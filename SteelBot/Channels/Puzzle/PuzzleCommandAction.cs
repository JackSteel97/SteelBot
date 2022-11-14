using DSharpPlus.Entities;
using SteelBot.Responders;

namespace SteelBot.Channels.Puzzle;

public enum PuzzleCommandActionType
{
    Answer,
    Puzzle,
    Clue
}
public record PuzzleCommandAction : BaseAction<PuzzleCommandActionType>
{
    public string GivenAnswer { get; }
    public PuzzleCommandAction(PuzzleCommandActionType action, IResponder responder, DiscordMember member, DiscordGuild guild, string givenAnswer)
        : base(action, responder, member, guild)
    {
        GivenAnswer = givenAnswer;
    }
    
    public PuzzleCommandAction(PuzzleCommandActionType action, IResponder responder, DiscordMember member, DiscordGuild guild)
        : base(action, responder, member, guild)
    {
    }
}