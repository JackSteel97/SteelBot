using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Humanizer;
using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Models;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteelBot.DiscordModules.Pets.Helpers;

public static class PetDisplayHelpers
{
    public static DiscordEmbedBuilder GetPetDisplayEmbed(Pet pet, bool includeName = true, bool showLevelProgress = false)
    {
        string name = includeName ? $"{pet.GetName().ToZalgo(pet.IsCorrupt)} - " : $"{pet.Species.GetName().ToZalgo(pet.IsCorrupt)} - ";
        var embedBuilder = new DiscordEmbedBuilder()
            .WithColor(new DiscordColor(pet.Rarity.GetColour(pet.IsCorrupt)))
            .WithTitle($"{name}Level {pet.CurrentLevel}")
            .WithTimestamp(DateTime.Now)
            .AddField("Rarity", Formatter.InlineCode(pet.Rarity.ToString().ToZalgo(pet.IsCorrupt)), true)
            .AddField("Species", Formatter.InlineCode(pet.Species.GetName().ToZalgo(pet.IsCorrupt)), true)
            .AddField("Size", Formatter.InlineCode(pet.Size.ToString().ToZalgo(pet.IsCorrupt)), true)
            .AddField("Age", Formatter.InlineCode($"{GetAge(pet.BornAt)}"), true)
            .AddField("Found", Formatter.InlineCode(pet.FoundAt.Humanize()), true);

        if (showLevelProgress)
        {
            embedBuilder.WithDescription(PetShared.GetPetLevelProgressBar(pet));
        }

        foreach (var attribute in pet.Attributes)
        {
            embedBuilder.AddField(attribute.Name, Formatter.InlineCode(attribute.Description.ToZalgo(pet.IsCorrupt)), true);
        }

        var bonuses = AppendBonuses(new StringBuilder(), pet);
        string bonusList = bonuses.ToString();
        if (!string.IsNullOrWhiteSpace(bonusList))
        {
            embedBuilder.AddField("Bonuses", bonuses.ToString());
        }

        return embedBuilder;
    }

    public static List<Page> GetPetBonusesSummary(List<PetWithActivation> allPets, string username, string avatarUrl, double baseCapacity, double maxCapacity)
    {
        var embedBuilder = new DiscordEmbedBuilder().WithColor(EmbedGenerator.InfoColour)
            .WithTitle($"{username} Pet's Active Bonuses")
            .WithThumbnail(avatarUrl)
            .WithTimestamp(DateTime.Now);

        var totals = new BonusTotals();
        bool anyDisabled = false;
        foreach (var pet in allPets)
        {
            if (pet.Active)
            {
                totals.Add(pet.Pet);
            }
            else
            {
                if (!anyDisabled)
                {
                    anyDisabled = true;
                }
            }
        }

        if (anyDisabled)
        {
            embedBuilder.WithFooter("Inactive pet's bonuses have no effect until you reach the required level in this server or activate bonus pet slots.");
        }

        var totalsBuilder = new StringBuilder();
        AppendBonuses(totalsBuilder, totals, false);
        embedBuilder.AddField("Totals", totalsBuilder.ToString());

        return PaginationHelper.GenerateEmbedPages(embedBuilder, allPets, 5, (builder, petWithActivation, _) =>
        {
            var pet = petWithActivation.Pet;
            builder.AppendLine(Formatter.Bold((pet.Priority + 1).Ordinalize()))
            .Append(Formatter.Bold(pet.GetName().ToZalgo(pet.IsCorrupt))).Append(" - Level ").Append(pet.CurrentLevel).Append(' ').Append(Formatter.Italic(pet.Rarity.ToString().ToZalgo(pet.IsCorrupt))).Append(' ').Append(pet.Species.GetName().ToZalgo(pet.IsCorrupt));
            if (!petWithActivation.Active)
            {
                int levelRequired = PetShared.GetRequiredLevelForPet(pet.Priority, baseCapacity, maxCapacity);
                builder.Append(" - **Inactive**, Level ").Append(levelRequired).Append(" required");
            }
            builder.AppendLine();
            return AppendBonuses(builder, pet);
        });
    }

    public static StringBuilder AppendBonusDisplay(StringBuilder builder, PetBonus bonus, bool isCorrupt = false) => AppendBonus(builder, bonus, isCorrupt);

    private static string GetAge(DateTime birthdate)
    {
        var age = DateTime.UtcNow - birthdate;
        string ageStr = age.Humanize(maxUnit: Humanizer.Localisation.TimeUnit.Year);
        return string.Concat(ageStr, " old");
    }

    private static StringBuilder AppendBonuses(StringBuilder bonuses, BonusTotals totals, bool isCorrupt)
    {
        foreach (var bonus in totals.Totals.Values.OrderBy(x => x.BonusType))
        {
            AppendBonus(bonuses, bonus, isCorrupt);
        }
        return bonuses;
    }

    private static StringBuilder AppendBonuses(StringBuilder bonuses, Pet pet)
    {
        var bonusTotals = new BonusTotals(pet);
        AppendBonuses(bonuses, bonusTotals, pet.IsCorrupt);

        return bonuses;
    }

    private static StringBuilder AppendBonus(StringBuilder bonuses, PetBonus bonus, bool isCorrupt)
    {
        string emoji = GetEmoji(bonus.Value, bonus.BonusType.IsNegative());
        double bonusValue = bonus.Value;

        string bonusValueFormat = bonus.BonusType.IsPercentage() ? "P2" : "N2";
        if (bonus.BonusType.IsRounded())
        {
            bonusValue = Math.Round(bonusValue);
            bonusValueFormat = "N0";
        }

        if (bonusValue != 0)
        {
            char bonusSign = char.MinValue;
            if (bonusValue >= 0)
            {
                bonusSign = '+';
            }

            string bonusSuffix = "";
            if (bonus.BonusType == BonusType.PetSlots && bonusValue > 50)
            {
                bonusSuffix = " (Capped at +50)";
            }

            bonuses.Append(emoji).Append(" - ").Append('`').Append(bonus.BonusType.Humanize().Titleize().ToZalgo(isCorrupt)).Append(": ").Append(bonusSign).Append(bonusValue.ToString(bonusValueFormat)).Append('`').AppendLine(bonusSuffix);
        }
        return bonuses;
    }

    private static string GetEmoji(double value, bool isNegativeType = false)
    {
        string emoji = EmojiConstants.CustomDiscordEmojis.GreenArrowUp;
        if ((isNegativeType && value > 0) || (!isNegativeType && value <= 0))
        {
            emoji = EmojiConstants.CustomDiscordEmojis.RedArrowDown;
        }
        return emoji;
    }
}