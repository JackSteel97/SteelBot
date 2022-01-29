using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using SteelBot.DataProviders;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using DSharpPlus;
using SteelBot.Helpers.Extensions;
using Humanizer;
using SteelBot.Helpers;

namespace SteelBot.DiscordModules.Pets
{
    public class PetsDataHelper
    {
        private readonly DataCache Cache;
        private readonly AppConfigurationService AppConfigurationService;
        private readonly ILogger<PetsDataHelper> Logger;

        public PetsDataHelper(DataCache cache, AppConfigurationService appConfigurationService, ILogger<PetsDataHelper> logger)
        {
            Cache = cache;
            AppConfigurationService = appConfigurationService;
            Logger = logger;
        }

        public DiscordMessageBuilder GetPetRanAwayMessage(Pet pet)
        {
            var embedBuilder = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(pet.Rarity.GetColour()))
                .WithTitle("It got away!")
                .WithDescription($"The {pet.Species.GetName()} ran away before you could befriend it.\nBetter luck next time!");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }
        public DiscordEmbedBuilder GetPetDisplayEmbed(Pet pet, bool includeName = true)
        {
            var name = "";
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
                .AddField("Rarity", Formatter.InlineCode(pet.Rarity.ToString()), true)
                .AddField("Species", Formatter.InlineCode(pet.Species.GetName()), true)
                .AddField("Size", Formatter.InlineCode(pet.Size.ToString()), true)
                .AddField("Age", Formatter.InlineCode($"{GetAge(pet.BornAt)}"), true)
                .AddField("Found", Formatter.InlineCode(pet.FoundAt.Humanize()), true);

            foreach(var attribute in pet.Attributes)
            {
                embedBuilder.AddField(attribute.Name, Formatter.InlineCode(attribute.Description), true);
            }

            StringBuilder bonuses = new StringBuilder();
            foreach(var bonus in pet.Bonuses)
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

            embedBuilder.AddField("Bonuses", bonuses.ToString());

            return embedBuilder;
        }

        public bool SearchSuccess(DiscordMember userSearching)
        {
            var probability = GetSearchSuccessProbability(userSearching);
            return MathsHelper.TrueWithProbability(probability);
        }

        public bool HasSpaceForAnotherPet(DiscordMember user)
        {
            // TODO: Implement check based on number of existing pets.
            return true;
        }

        private static string GetAge(DateTime birthdate)
        {
            var age = DateTime.UtcNow - birthdate;
            var ageStr = age.Humanize(maxUnit: Humanizer.Localisation.TimeUnit.Year);
            return string.Concat(ageStr, " old");
        }

        private double GetSearchSuccessProbability(DiscordMember userSearching)
        {
            // TODO: Implement dynamic probability based on number of existing pets etc.
            return 1;
        }
    }
}
