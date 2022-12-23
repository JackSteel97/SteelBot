using System;

namespace SteelBot.DiscordModules.Pets.Enums;

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
    private const string _common = "#808080";
    private const string _uncommon = "#008000";
    private const string _rare = "#0F52BA";
    private const string _epic = "#7F00FF";
    private const string _legendary = "#FF5F1F";
    private const string _mythical = "#DC2367";
    private const string _corrupt = "#B80F0A";

    public static string GetColour(this Rarity rarity, bool isCorrupt) =>
        isCorrupt
            ? _corrupt
            : rarity switch
            {
                Rarity.Common => _common,
                Rarity.Uncommon => _uncommon,
                Rarity.Rare => _rare,
                Rarity.Epic => _epic,
                Rarity.Legendary => _legendary,
                Rarity.Mythical => _mythical,
                _ => throw new ArgumentOutOfRangeException(nameof(rarity))
            };

    public static int GetStartingBonusCount(this Rarity rarity) =>
        rarity switch
        {
            Rarity.Common => 1,
            Rarity.Uncommon => 1,
            Rarity.Rare => 2,
            Rarity.Epic => 3,
            Rarity.Legendary => 3,
            Rarity.Mythical => 5,
            _ => throw new ArgumentOutOfRangeException(nameof(rarity), $"Value {rarity} not valid")
        };

    public static double GetMaxBonusValue(this Rarity rarity) =>
        rarity switch
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