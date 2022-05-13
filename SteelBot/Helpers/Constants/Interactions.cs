using DSharpPlus;
using DSharpPlus.Entities;

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
            public static readonly DiscordButtonComponent Rename = new(ButtonStyle.Primary, InteractionIds.Pets.Rename, "Rename", emoji: new DiscordComponentEmoji(EmojiConstants.Objects.WritingHand));
            public static readonly DiscordButtonComponent Abandon = new(ButtonStyle.Danger, InteractionIds.Pets.Abandon, "Release into the wild", emoji: new DiscordComponentEmoji(EmojiConstants.Objects.Herb));
            public static readonly DiscordButtonComponent MakePrimary = new(ButtonStyle.Success, InteractionIds.Pets.MakePrimary, "Make Primary", emoji: new DiscordComponentEmoji(EmojiConstants.Symbols.GlowingStar));
            public static readonly DiscordButtonComponent IncreasePriority = new(ButtonStyle.Secondary, InteractionIds.Pets.IncreasePriority, "Move Up", emoji: new DiscordComponentEmoji(EmojiConstants.Symbols.UpButton));
            public static readonly DiscordButtonComponent DecreasePriority = new(ButtonStyle.Secondary, InteractionIds.Pets.DecreasePriority, "Move Down", emoji: new DiscordComponentEmoji(EmojiConstants.Symbols.DownButton));
            public static readonly DiscordButtonComponent MoveToBottom = new(ButtonStyle.Secondary, InteractionIds.Pets.MoveToBottom, "Move To Bottom", emoji: new DiscordComponentEmoji(EmojiConstants.Faces.Pleading));
            public static readonly DiscordButtonComponent MoveToPosition = new(ButtonStyle.Secondary, InteractionIds.Pets.MoveToButton, "Move To Position", emoji: new DiscordComponentEmoji(EmojiConstants.Symbols.LeftRightArrow));

            public static TextInputComponent NameInput(long petId, string placeholderSuffix)
            {
                return new TextInputComponent("Name", $"{InteractionIds.Pets.NameInput}:{petId}", $"Name this {placeholderSuffix}...", min_length: 1, max_length: 70);
            }

            public static TextInputComponent MovePositionInput(long petId, int maxInput)
            {
                return new TextInputComponent("Move To Position", $"{InteractionIds.Pets.MovePositionInput}:{petId}", $"Enter a position between 1 and {maxInput}...", min_length: 1, max_length: 2);
            }

            public static DiscordButtonComponent Manage(long petId, string name)
            {
                return new DiscordButtonComponent(ButtonStyle.Primary, $"{InteractionIds.Pets.Manage}:{petId}", $"Manage {name}");
            }

            public static DiscordButtonComponent Replace(long petId, string name)
            {
                return new DiscordButtonComponent(ButtonStyle.Danger, $"{InteractionIds.Pets.Replace}:{petId}", $"Replace {name}");
            }
            public static DiscordButtonComponent Treat(long petId, string name)
            {
                return new DiscordButtonComponent(ButtonStyle.Success, $"{InteractionIds.Pets.Treat}:{petId}", $"Treat {name}");
            }
        }

        public static class Confirmation
        {
            public static readonly DiscordButtonComponent Confirm = new(ButtonStyle.Primary, InteractionIds.Confirmation.Confirm, "Confirm", emoji: new DiscordComponentEmoji(EmojiConstants.Symbols.CheckMarkButton));
            public static readonly DiscordButtonComponent Cancel = new(ButtonStyle.Secondary, InteractionIds.Confirmation.Cancel, "Cancel", emoji: new DiscordComponentEmoji(EmojiConstants.Symbols.CrossMark));
        }

        public static class Pagination
        {
            public static readonly DiscordButtonComponent NextPage = new(ButtonStyle.Secondary, InteractionIds.Pagination.NextPage, "", emoji: new DiscordComponentEmoji(EmojiConstants.Symbols.ForwardArrow));
            public static readonly DiscordButtonComponent PrevPage = new(ButtonStyle.Secondary, InteractionIds.Pagination.PrevPage, "", emoji: new DiscordComponentEmoji(EmojiConstants.Symbols.BackwardArrow));
            public static readonly DiscordButtonComponent Exit = new(ButtonStyle.Secondary, InteractionIds.Pagination.Exit, "", emoji: new DiscordComponentEmoji(EmojiConstants.Symbols.StopButton));
        }
    }

    public static class InteractionIds
    {
        public static class Modals
        {
            public const string PetNameEntry = "name_entry_pet";
            public const string PetMove = "move_pet";
        }

        public static class Pets
        {
            public const string Befriend = "befriend_pet";
            public const string Leave = "leave_pet";
            public const string NameConfirm = "name_confirmed_pet";
            public const string NameRetry = "name_retry_pet";
            public const string Manage = "manage_pet";
            public const string Replace = "replace_pet";
            public const string Rename = "rename_pet";
            public const string Abandon = "abandon_pet";
            public const string MakePrimary = "primary_pet";
            public const string IncreasePriority = "increase_priority_pet";
            public const string DecreasePriority = "decrease_priority_pet";
            public const string MoveToBottom = "move_to_bottom_pet";
            public const string Treat = "treat_pet";
            public const string NameInput = "name_input_pet";
            public const string MovePositionInput = "move_position_pet";
            public const string MoveToButton = "move_to_pet";
        }

        public static class Confirmation
        {
            public const string Confirm = "confirmed";
            public const string Cancel = "cancelled";
        }

        public static class Pagination
        {
            public const string NextPage = "next_page";
            public const string PrevPage = "prev_page";
            public const string Exit = "exit_page";
        }
    }
}
