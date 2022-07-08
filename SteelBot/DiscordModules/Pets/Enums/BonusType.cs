using System;
using System.ComponentModel;

namespace SteelBot.DiscordModules.Pets.Enums;

[Flags]
public enum BonusType
{
    None = 0,
    
    [Description("Message XP")]
    MessageXp = 1,
    
    [Description("Voice XP")]
    VoiceXp = 1 << 1,
    
    [Description("Streaming XP")]
    StreamingXp = 1 << 2,
    
    [Description("Video XP")]
    VideoXp = 1 << 3,
    
    [Description("Muted Penalty XP")]
    MutedPenaltyXp = 1 << 4,
    
    [Description("Deafened Penalty XP")]
    DeafenedPenaltyXp = 1 << 5,
    
    SearchSuccessRate = 1 << 6,
    BefriendSuccessRate = 1 << 7,
    PetSlots = 1 << 8,
    
    [Description("Pet Shared XP")]
    PetSharedXp = 1 << 9,
    
    [Description("Offline XP")]
    OfflineXp = 1 << 10,
    
    [Description("Pet Treat XP")]
    PetTreatXp = 1 << 11,
    
    [Description("All XP")]
    AllXp = MessageXp | VoiceXp | StreamingXp | VideoXp
}

public static class BonusTypeExtensions
{
    public static bool IsPenalty(this BonusType type) => type.HasFlag(BonusType.MutedPenaltyXp) || type.HasFlag(BonusType.DeafenedPenaltyXp);

    public static bool IsPercentage(this BonusType type)
    {
        return type switch
        {
            BonusType.PetSlots => false,
            BonusType.OfflineXp => false,
            _ => true,
        };
    }

    public static bool IsRounded(this BonusType type)
    {
        return type switch
        {
            BonusType.PetSlots => true,
            _ => false
        };
    }
}