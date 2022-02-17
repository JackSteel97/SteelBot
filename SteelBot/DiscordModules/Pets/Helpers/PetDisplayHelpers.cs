using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Helpers
{
    public class PetDisplayHelpers
    {
        public static DiscordEmbedBuilder GetPetDisplayEmbed(Pet pet, bool includeName = true)
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

            foreach (var attribute in pet.Attributes)
            {
                embedBuilder.AddField(attribute.Name, Formatter.InlineCode(attribute.Description), true);
            }

            StringBuilder bonuses = new StringBuilder();
            foreach (var bonus in pet.Bonuses)
            {
                bonuses.Append('`').Append(bonus.BonusType.Humanize()).Append(" XP").Append(": ");
                if (bonus.BonusType.IsNegative())
                {
                    bonuses.Append('-');
                }
                else
                {
                    bonuses.Append('+');
                }
                bonuses.Append(bonus.PercentageValue.ToString("P2")).AppendLine("`");
            }

            if (pet.Rarity == Rarity.Legendary)
            {
                bonuses.Append("`Passive Offline XP: +").Append(pet.CurrentLevel).Append('`');
            }

            embedBuilder.AddField("Bonuses", bonuses.ToString());

            return embedBuilder;
        }

        private static string GetAge(DateTime birthdate)
        {
            var age = DateTime.UtcNow - birthdate;
            var ageStr = age.Humanize(maxUnit: Humanizer.Localisation.TimeUnit.Year);
            return string.Concat(ageStr, " old");
        }
    }
}
