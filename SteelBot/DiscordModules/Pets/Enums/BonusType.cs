﻿using System;

namespace SteelBot.DiscordModules.Pets.Enums
{
    [Flags]
    public enum BonusType
    {
        None = 0,
        MessageXP = 1,
        VoiceXP = 1 << 1,
        StreamingXP = 1 << 2,
        VideoXP = 1 << 3,
        MutedPentaltyXP = 1 << 4,
        DeafenedPenaltyXp = 1 << 5,
        SearchSuccessRate = 1 << 6,
        BefriendSuccessRate = 1 << 7,
        PetSlots = 1 << 8,
        AllXP = MessageXP | VoiceXP | StreamingXP | VideoXP
    }

    public static class BonusTypeExtensions
    {
        public static bool IsNegative(this BonusType type)
        {
            return type.HasFlag(BonusType.MutedPentaltyXP) || type.HasFlag(BonusType.DeafenedPenaltyXp);
        }

        public static bool IsPercentage(this BonusType type)
        {
            return type switch
            {
                BonusType.PetSlots => false,
                _ => true,
            };
        }
    }
}
