using DSharpPlus.Entities;
using SteelBot.Responders;

namespace SteelBot.Channels;

public record BaseAction<TAction>(TAction Action, IResponder Responder, DiscordMember Member, DiscordGuild Guild);