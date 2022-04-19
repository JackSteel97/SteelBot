using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace SteelBot.Channels.Message
{
    public class IncomingMessage
    {
        public DiscordGuild Guild { get; init; }
        public DiscordUser User { get; init; }
        public DiscordMessage Message { get; init; }

        public IncomingMessage(DiscordGuild guild, DiscordUser user, DiscordMessage message)
        {
            Guild = guild;
            User = user;
            Message = message;
        }

        public IncomingMessage(MessageCreateEventArgs args)
        {
            Guild = args.Guild;
            User = args.Author;
            Message = args.Message;
        }
    }
}
