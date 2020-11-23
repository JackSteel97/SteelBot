using DSharpPlus.Entities;

namespace SteelBot.Helpers
{
    public static class EmbedGenerator
    {
        public static readonly DiscordColor ErrorColour = new DiscordColor(0xD0021B);
        public static readonly DiscordColor WarningColour = new DiscordColor(0xF5A623);
        public static readonly DiscordColor SuccessColour = new DiscordColor(0x36D321);
        public static readonly DiscordColor PrimaryColour = new DiscordColor(0x242424);
        public static readonly DiscordColor InfoColour = new DiscordColor(0x4A90E2);

        public static DiscordEmbed Error(string errorMessage)
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(ErrorColour)
                .WithTitle("Error")
                .WithDescription(errorMessage);
            return builder.Build();
        }

        public static DiscordEmbed Warning(string warningMessage)
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(WarningColour)
                .WithTitle("Warning")
                .WithDescription(warningMessage);
            return builder.Build();
        }

        public static DiscordEmbed Success(string successMessage)
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(SuccessColour)
                .WithTitle("Success")
                .WithDescription(successMessage);
            return builder.Build();
        }

        public static DiscordEmbed Primary(string message, string title = "")
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(PrimaryColour)
                .WithDescription(message);
            if (!string.IsNullOrWhiteSpace(title))
            {
                builder = builder.WithTitle(title);
            }
            return builder.Build();
        }

        public static DiscordEmbed Info(string message, string title = "")
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(InfoColour)
                .WithDescription(message);
            if (!string.IsNullOrWhiteSpace(title))
            {
                builder = builder.WithTitle(title);
            }
            return builder.Build();
        }
    }
}