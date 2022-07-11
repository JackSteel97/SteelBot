using DSharpPlus.Entities;
using SteelBot.Responders;

namespace SteelBot.Channels.Pets;

public enum PetCommandActionType
{
    Search,
    ManageAll,
    Treat,
    ManageOne,
    Order,
    ViewBonuses,
    ViewPets
}
public class PetCommandAction
{
    public PetCommandActionType Action { get; init; }
    public IResponder Responder { get; init; }
    public DiscordMember Member { get; init; }
    public DiscordGuild Guild { get; init; }

    public PetCommandAction(PetCommandActionType action, IResponder responder, DiscordMember member, DiscordGuild guild)
    {
        Action = action;
        Responder = responder;
        Member = member;
        Guild = guild;
    }
}