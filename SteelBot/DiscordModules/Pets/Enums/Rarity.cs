using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Enums
{
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public static class RarityExtensions
    {
        private const string Common = "#808080";
        private const string Uncommon = "#008000";
        private const string Rare = "#0F52BA";
        private const string Epic = "#7F00FF";
        private const string Legendary = "#FF5F1F";

        public static string GetColour(this Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Common => Common,
                Rarity.Uncommon => Uncommon,
                Rarity.Rare => Rare,
                Rarity.Epic => Epic,
                Rarity.Legendary => Legendary,
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
                Rarity.Epic => 2,
                Rarity.Legendary => 3,
                _ => throw new ArgumentOutOfRangeException(nameof(rarity))
            };
        }
    }
}
