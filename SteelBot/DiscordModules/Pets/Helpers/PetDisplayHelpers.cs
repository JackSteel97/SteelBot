﻿using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Models;
using SteelBot.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace SteelBot.DiscordModules.Pets.Helpers
{
    public static class PetDisplayHelpers
    {
        public static DiscordEmbedBuilder GetPetDisplayEmbed(Pet pet, bool includeName = true, bool showLevelProgress = false)
        {
            string name;
            if (includeName)
            {
                name = $"{pet.Name ?? $"Unamed {pet.Species.GetName()}"} - ";
            }
            else
            {
                name = $"{pet.Species.GetName()} - ";
            }
            var embedBuilder = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(pet.Rarity.GetColour()))
                .WithTitle($"{name}Level {pet.CurrentLevel}")
                .WithTimestamp(DateTime.Now)
                .AddField("Rarity", Formatter.InlineCode(pet.Rarity.ToString()), true)
                .AddField("Species", Formatter.InlineCode(pet.Species.GetName()), true)
                .AddField("Size", Formatter.InlineCode(pet.Size.ToString()), true)
                .AddField("Age", Formatter.InlineCode($"{GetAge(pet.BornAt)}"), true)
                .AddField("Found", Formatter.InlineCode(pet.FoundAt.Humanize()), true);

            if (showLevelProgress)
            {
                embedBuilder.WithDescription(PetShared.GetPetLevelProgressBar(pet));
            }

            foreach (var attribute in pet.Attributes)
            {
                embedBuilder.AddField(attribute.Name, Formatter.InlineCode(attribute.Description), true);
            }

            StringBuilder bonuses = AppendBonuses(new StringBuilder(), pet);

            embedBuilder.AddField("Bonuses", bonuses.ToString());

            return embedBuilder;
        }

        public static DiscordEmbedBuilder GetPetBonusesSummary(List<Pet> availablePets, string username)
        {
            var embedBuilder = new DiscordEmbedBuilder().WithColor(EmbedGenerator.InfoColour)
                .WithTitle($"{username} Pet's Bonuses")
                .WithTimestamp(DateTime.Now);

            var totals = new BonusTotals();
            var bonuses = new StringBuilder();
            foreach (var pet in availablePets)
            {
                AppendBonuses(bonuses, pet);
                embedBuilder.AddField(pet.GetName(), bonuses.ToString());
                bonuses.Clear();
                totals.Add(pet);
            }

            embedBuilder.AddField("Totals", totals.ToString());
            return embedBuilder;
        }

        private static string GetAge(DateTime birthdate)
        {
            var age = DateTime.UtcNow - birthdate;
            var ageStr = age.Humanize(maxUnit: Humanizer.Localisation.TimeUnit.Year);
            return string.Concat(ageStr, " old");
        }

        private static StringBuilder AppendBonuses(StringBuilder bonuses, Pet pet)
        {
            foreach (var bonus in pet.Bonuses)
            {
                AppendBonus(bonuses, bonus);
            }

            if (pet.Rarity == Rarity.Legendary)
            {
                AppendLegendaryBonus(bonuses, pet.CurrentLevel);
            }
            else if (pet.Rarity == Rarity.Mythical)
            {
                AppendMythicalBonus(bonuses, pet.CurrentLevel);
            }
            return bonuses;
        }

        private static StringBuilder AppendBonus(StringBuilder bonuses, PetBonus bonus)
        {
            var emoji = EmojiConstants.CustomDiscordEmojis.GreenArrowUp;
            var isNegativeType = bonus.BonusType.IsNegative();
            if((isNegativeType && bonus.Value > 0) || (!isNegativeType && bonus.Value <= 0))
            {
                emoji = EmojiConstants.CustomDiscordEmojis.RedArrowDown;
            }
            bonuses.Append(emoji).Append(" - ").Append('`').Append(bonus.BonusType.Humanize()).Append(" XP").Append(": ").Append(bonus.Value.ToString("P2")).AppendLine("`");
            return bonuses;
        }

        private static StringBuilder AppendLegendaryBonus(StringBuilder bonuses, int currentLevel)
        {
            return bonuses.Append(EmojiConstants.CustomDiscordEmojis.GreenArrowUp).Append(" - ").Append("`Passive Offline XP: +").Append(currentLevel).Append('`');
        }

        private static StringBuilder AppendMythicalBonus(StringBuilder bonuses, int currentLevel)
        {
            return bonuses.Append(EmojiConstants.CustomDiscordEmojis.GreenArrowUp).Append(" - ").Append("`Passive Offline XP +").Append(currentLevel * 2).Append('`');
        }
    }
}
