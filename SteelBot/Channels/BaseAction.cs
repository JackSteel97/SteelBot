using DSharpPlus.Entities;
using SteelBot.Responders;

namespace SteelBot.Channels;

public class BaseAction<TAction> 
{
    public TAction Action { get; }
    public IResponder Responder { get; }
    public DiscordMember Member { get; }
    public DiscordGuild Guild { get; }

    public BaseAction(TAction action, IResponder responder, DiscordMember member, DiscordGuild guild)
    {
        Action = action;
        Responder = responder;
        Member = member;
        Guild = guild;
    }
}