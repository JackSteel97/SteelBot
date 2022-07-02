using System;

namespace SteelBot.DiscordModules.Pets.Enums;

[Flags]
public enum BonusType
{
    None = 0,
    MessageXp = 1,
    VoiceXp = 1 << 1,
    StreamingXp = 1 << 2,
    VideoXp = 1 << 3,
    MutedPenaltyXp = 1 << 4,
    DeafenedPenaltyXp = 1 << 5,
    SearchSuccessRate = 1 << 6,
    BefriendSuccessRate = 1 << 7,
    PetSlots = 1 << 8,
    PetSharedXp = 1 << 9,
    OfflineXp = 1 << 10,
    AllXp = MessageXp | VoiceXp | StreamingXp | VideoXp
}

public static class BonusTypeExtensions
{
    public static bool IsNegative(this BonusType type) => type.HasFlag(BonusType.MutedPenaltyXp) || type.HasFlag(BonusType.DeafenedPenaltyXp);

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