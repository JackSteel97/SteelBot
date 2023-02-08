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
    View,
    CheckForDeath
}

public record PetCommandAction : BaseAction<PetCommandActionType>
{
    public DiscordMember Target { get; }
    public long PetId { get; init; }

    public PetCommandAction(PetCommandActionType action, IResponder responder, DiscordMember member, DiscordGuild guild, DiscordMember target = null)
        : base(action, responder, member, guild)
    {
        Target = target ?? member;
    }

    public PetCommandAction(PetCommandActionType action, IResponder responder, DiscordMember member, DiscordGuild guild, long petId, DiscordMember target = null)
        : this(action, responder, member, guild, target)
    {
        PetId = petId;
    }
}