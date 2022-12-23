namespace SteelBot.Services.Configuration;

public class LevellingConfig
{
    public double VoiceXpPerMin { get; set; }
    public double MutedXpPerMin { get; set; }
    public double DeafenedXpPerMin { get; set; }
    public double StreamingXpPerMin { get; set; }
    public double VideoXpPerMin { get; set; }
    public ulong MessageXp { get; set; }
}