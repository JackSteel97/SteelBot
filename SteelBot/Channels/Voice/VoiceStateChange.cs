using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace SteelBot.Channels.Voice
{
    public class VoiceStateChange
    {
        public DiscordGuild Guild { get; init; }
        public DiscordUser User { get; init; }
        public DiscordVoiceState Before { get; init; }
        public DiscordVoiceState After { get; init; }

        public VoiceStateChange(DiscordGuild guild, DiscordUser user, DiscordVoiceState before, DiscordVoiceState after)
        {
            Guild = guild;
            User = user;
            Before = before;
            After = after;
        }

        public VoiceStateChange(DiscordGuild guild, DiscordUser user, DiscordVoiceState after)
        {
            Guild = guild;
            User = user;
            After = after;
        }

        public VoiceStateChange(VoiceStateUpdateEventArgs args)
        {
            Guild = args.Guild;
            User = args.User;
            Before = args.Before;
            After = args.After;
        }
    }
}
