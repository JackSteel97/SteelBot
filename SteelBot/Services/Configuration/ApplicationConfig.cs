namespace SteelBot.Services.Configuration
{
    public class ApplicationConfig
    {
        public DiscordConfig Discord { get; set; }
        public string DefaultCommandPrefix { get; set; }
        public string UnknownCommandResponse { get; set; }
        public ulong CommonServerId { get; set; }
        public ulong CreatorUserId { get; set; }
        public string AlphaVantageApiKey { get; set; }
        public int StockCacheTimeMinutes { get; set; }
        public string InviteLink { get; set; }
        public LevellingConfig Levelling { get; set; }
    }
}