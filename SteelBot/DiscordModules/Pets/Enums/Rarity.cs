using System;

namespace SteelBot.DiscordModules.Pets.Enums
{
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythical
    }

    public static class RarityExtensions
    {
        private const string Common = "#808080";
        private const string Uncommon = "#008000";
        private const string Rare = "#0F52BA";
        private const string Epic = "#7F00FF";
        private const string Legendary = "#FF5F1F";
        private const string Mythical = "#DC2367";

        public static string GetColour(this Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Common => Common,
                Rarity.Uncommon => Uncommon,
                Rarity.Rare => Rare,
                Rarity.Epic => Epic,
                Rarity.Legendary => Legendary,
                Rarity.Mythical => Mythical,
                _ => throw new ArgumentOutOfRangeException(nameof(rarity)),
            };
        }

        public static int GetStartingBonusCount(this Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Common => 1,
                Rarity.Uncommon => 1,
                Rarity.Rare => 2,
                Rarity.Epic => 3,
                Rarity.Legendary => 3,
                Rarity.Mythical => 5,
                _ => throw new ArgumentOutOfRangeException(nameof(rarity), $"Value {rarity} not valid")
            };
        }

        public static double GetMaxBonusValue(this Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Common => 0.2,
                Rarity.Uncommon => 0.3,
                Rarity.Rare => 0.4,
                Rarity.Epic => 0.6,
                Rarity.Legendary => 0.8,
                Rarity.Mythical => 1,
                _ => throw new ArgumentOutOfRangeException(nameof(rarity), $"Value {rarity} not valid")
            };
        }
    }
}
