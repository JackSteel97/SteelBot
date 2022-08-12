using DSharpPlus.Entities;
using SteelBot.Responders;

namespace SteelBot.Channels.Pets;

public enum PetCommandActionType
{
    Search,
    ManageAll,
    Treat,
    ManageOne,
    ViewBonuses,
    View
}
public class PetCommandAction
{
    public PetCommandActionType Action { get; init; }
    public IResponder Responder { get; init; }
    public DiscordMember Member { get; init; }
    public DiscordMember Target { get; init; }
    public DiscordGuild Guild { get; init; }
    public long PetId { get; init; }

    public PetCommandAction(PetCommandActionType action, IResponder responder, DiscordMember member, DiscordGuild guild, DiscordMember target = null)
    {
        Action = action;
        Responder = responder;
        Member = member;
        Guild = guild;
        Target = target ?? member;
    }

    public PetCommandAction(PetCommandActionType action, IResponder responder, DiscordMember member, DiscordGuild guild, long petId, DiscordMember target = null)
    : this(action, responder, member, guild, target)
    {
        PetId = petId;
    }
}