using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.Helpers.Constants
{
    public static class Interactions
    {
        public static class Pets
        {
            public static readonly DiscordButtonComponent Befriend = new(ButtonStyle.Primary, InteractionIds.Pets.Befriend, "Befriend", emoji: new DiscordComponentEmoji(EmojiConstants.Faces.Hugging));
            public static readonly DiscordButtonComponent Leave = new(ButtonStyle.Secondary, InteractionIds.Pets.Leave, "Leave", emoji: new DiscordComponentEmoji(EmojiConstants.Objects.WavingHand));
            public static readonly DiscordButtonComponent NameConfirm = new(ButtonStyle.Success, InteractionIds.Pets.NameConfirm, "Yes", emoji: new DiscordComponentEmoji(EmojiConstants.Objects.ThumbsUp));
            public static readonly DiscordButtonComponent NameRetry = new(ButtonStyle.Danger, InteractionIds.Pets.NameRetry, "Pick a different name", emoji: new DiscordComponentEmoji(EmojiConstants.Objects.ThumbsDown));
        }
    }

    public static class InteractionIds
    {
        public static class Pets
        {
            public const string Befriend = "befriend_pet";
            public const string Leave = "leave_pet";
            public const string NameConfirm = "name_confirmed_pet";
            public const string NameRetry = "name_retry_pet";
        }
    }
}
