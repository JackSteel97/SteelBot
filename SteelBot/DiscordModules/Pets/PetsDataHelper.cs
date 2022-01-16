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

        public DiscordEmbedBuilder GetPetDisplayEmbed(Pet pet)
        {
            var embedBuilder = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(pet.Rarity.GetColour()))
                .WithTitle($"{pet.Name ?? $"Unamed {pet.Species.GetName()} - Level {pet.CurrentLevel}"}")
                .AddField("Rarity", Formatter.InlineCode(pet.Rarity.ToString()), true)
                .AddField("Species", Formatter.InlineCode(pet.Species.GetName()), true)
                .AddField("Size", Formatter.InlineCode(pet.Size.ToString()), true)
                .AddField("Age", Formatter.InlineCode($"{GetAge(pet.BornAt)}"), true)
                .AddField("Found", Formatter.InlineCode(pet.FoundAt.Humanize()), true);

            foreach(var attribute in pet.Attributes)
            {
                embedBuilder.AddField(attribute.Name, Formatter.InlineCode(attribute.Description), true);
            }

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
