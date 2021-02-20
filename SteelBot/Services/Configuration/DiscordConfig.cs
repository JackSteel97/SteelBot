namespace SteelBot.Services.Configuration
{
    public class DiscordConfig
    {
        public string LogLevel { get; set; }

        public int MessageCacheSize { get; set; }

        public bool CaseSensitiveCommands { get; set; }

        public bool AlwaysDownloadUsers { get; set; }

        public string LoginToken { get; set; }
    }
}