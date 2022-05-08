using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Humanizer;
using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Models;
using SteelBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
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
                name = $"{pet.GetName()} - ";
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
            var bonusList = bonuses.ToString();
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
            AppendBonuses(totalsBuilder, totals);
            embedBuilder.AddField("Totals", totalsBuilder.ToString());

            return PaginationHelper.GenerateEmbedPages(embedBuilder, allPets, 5, (builder, petWithActivation, _) =>
            {
                var pet = petWithActivation.Pet;
                builder.AppendLine(Formatter.Bold((pet.Priority+1).Ordinalize()))
                .Append(Formatter.Bold(pet.GetName())).Append(" - Level ").Append(pet.CurrentLevel).Append(' ').Append(Formatter.Italic(pet.Rarity.ToString())).Append(' ').Append(pet.Species.GetName());
                if (!petWithActivation.Active)
                {
                    var levelRequired = PetShared.GetRequiredLevelForPet(pet.Priority, baseCapacity, maxCapacity);
                    builder.Append(" - **Inactive**, Level ").Append(levelRequired).Append(" required");
                }
                builder.AppendLine();
                return AppendBonuses(builder, pet);
            });
        }

        public static StringBuilder AppendBonusDisplay(StringBuilder builder, PetBonus bonus)
        {
            return AppendBonus(builder, bonus);
        }

        private static string GetAge(DateTime birthdate)
        {
            var age = DateTime.UtcNow - birthdate;
            var ageStr = age.Humanize(maxUnit: Humanizer.Localisation.TimeUnit.Year);
            return string.Concat(ageStr, " old");
        }

        private static StringBuilder AppendBonuses(StringBuilder bonuses, BonusTotals totals)
        {
            foreach (var bonus in totals.Totals.Values.OrderBy(x => x.BonusType))
            {
                AppendBonus(bonuses, bonus);
            }
            return bonuses;
        }

        private static StringBuilder AppendBonuses(StringBuilder bonuses, Pet pet)
        {
            var bonusTotals = new BonusTotals(pet);
            AppendBonuses(bonuses, bonusTotals);

            return bonuses;
        }

        private static StringBuilder AppendBonus(StringBuilder bonuses, PetBonus bonus)
        {
            var emoji = GetEmoji(bonus.Value, bonus.BonusType.IsNegative());
            var bonusValue = bonus.Value;

            string bonusValueFormat = bonus.BonusType.IsPercentage() ? "P2" : "N2";
            if (bonus.BonusType.IsRounded())
            {
                bonusValue = Math.Round(bonusValue);
                bonusValueFormat = "N0";
            }

            if(bonusValue != 0)
            {
                char bonusSign = char.MinValue;
                if (bonusValue >= 0)
                {
                    bonusSign = '+';
                }

                string bonusSuffix = "";
                if(bonus.BonusType == BonusType.PetSlots && bonusValue > 50)
                {
                    bonusSuffix = " (Capped at +50)";
                }

                bonuses.Append(emoji).Append(" - ").Append('`').Append(bonus.BonusType.Humanize().Titleize()).Append(": ").Append(bonusSign).Append(bonusValue.ToString(bonusValueFormat)).Append('`').AppendLine(bonusSuffix);
            }
            return bonuses;
        }

        private static StringBuilder AppendPassiveXpBonus(StringBuilder bonuses, double passiveXp)
        {
            return bonuses.Append(EmojiConstants.CustomDiscordEmojis.GreenArrowUp).Append(" - ").Append("`Passive Offline XP: +").Append(passiveXp).AppendLine("`");
        }

        private static string GetEmoji(double value, bool isNegativeType = false)
        {
            var emoji = EmojiConstants.CustomDiscordEmojis.GreenArrowUp;
            if ((isNegativeType && value > 0) || (!isNegativeType && value <= 0))
            {
                emoji = EmojiConstants.CustomDiscordEmojis.RedArrowDown;
            }
            return emoji;
        }
    }
}
