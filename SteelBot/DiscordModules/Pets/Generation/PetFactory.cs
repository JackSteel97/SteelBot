using Humanizer;
using Microsoft.Extensions.Logging;
using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Generation
{
    public class PetFactory
    {
        private readonly ILogger<PetFactory> Logger;

        private readonly Dictionary<Rarity, List<Species>> SpeciesByRarity = new Dictionary<Rarity, List<Species>>();

        public PetFactory(ILogger<PetFactory> logger)
        {
            Logger = logger;
            BuildSpeciesCache();
        }

        private void BuildSpeciesCache()
        {
            var species = Enum.GetValues(typeof(Species)).Cast<Species>().ToArray();
            foreach (var spec in species)
            {
                var rarity = spec.GetRarity();
                if (!SpeciesByRarity.TryGetValue(rarity, out var includedSpecies))
                {
                    includedSpecies = new List<Species>();
                    SpeciesByRarity.Add(rarity, includedSpecies);
                }
                includedSpecies.Add(spec);
            }
        }

        public void Generate()
        {
            var baseRarity = GetBaseRarity();
            var species = GetSpecies(baseRarity);
            var size = GetRandomEnumValue<Size>();
            var birthDate = GetBirthDate(species);

            var pet = new Pet()
            {
                Rarity = baseRarity,
                Species = species,
                Size = size,
                BornAt = birthDate,
                FoundAt = DateTime.UtcNow
            };

            pet.Attributes = BuildAttributes(pet);
        }

        private static Rarity GetBaseRarity()
        {
            const int maxBound = 1000;
            const double LegendaryChance = 0.01;
            const double EpicChance = 0.03;
            const double RareChance = 0.06;
            const double UncommonChance = 0.3;

            const double LegendaryBound = maxBound * LegendaryChance;
            const double EpicBound = maxBound * EpicChance;
            const double RareBound = maxBound * RareChance;
            const double UncommonBound = maxBound * UncommonChance;

            int random = RandomNumberGenerator.GetInt32(maxBound);

            if (random < LegendaryBound)
            {
                return Rarity.Legendary;
            }
            else if (random < EpicBound)
            {
                return Rarity.Epic;
            }
            else if (random < RareBound)
            {
                return Rarity.Rare;
            }
            else if (random < UncommonBound)
            {
                return Rarity.Uncommon;
            }
            else
            {
                return Rarity.Common;
            }
        }

        private Species GetSpecies(Rarity rarity)
        {
            var possibleSpecies = SpeciesByRarity[rarity];
            int index = RandomNumberGenerator.GetInt32(possibleSpecies.Count);
            return possibleSpecies[index];
        }

        private static DateTime GetBirthDate(Species species)
        {
            int maxAgeMinutes = (int)Math.Floor(species.GetMaxStartingAge().TotalMinutes);
            var minutesOld = RandomNumberGenerator.GetInt32(30, maxAgeMinutes);
            return DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(minutesOld));
        }

        private static List<PetAttribute> BuildAttributes(Pet pet)
        {
            var bodyParts = pet.Species.GetBodyParts();

            var attributes = new List<PetAttribute>(bodyParts.Count);
            foreach (var part in bodyParts)
            {
                var attribute = new PetAttribute()
                {
                    Pet = pet,
                    Name = part.Humanize(),
                    Description = GenerateColourCombo()
                };
                attributes.Add(attribute);
            }
            return attributes;
        }

        private static string GenerateColourCombo()
        {
            if (OccurredWithProbability(0.1))
            {
                // Has two colours.
                var primary = GetRandomEnumValue<Colour>();
                var secondary = GetRandomEnumValue<Colour>();
                var mixing = GetRandomEnumValue<ColourMixing>();

                return $"{primary.Humanize()} and {secondary.Humanize()} {mixing.Humanize()}";
            }
            else
            {
                // Has one colour.
                var colour = GetRandomEnumValue<Colour>();
                return colour.Humanize();
            }
        }

        private static bool OccurredWithProbability(double probability)
        {
            const int maxBound = 1000;
            return RandomNumberGenerator.GetInt32(maxBound) < maxBound * probability;
        }


        private static T GetRandomEnumValue<T>()
        {
            var values = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
            return values[RandomNumberGenerator.GetInt32(values.Length)];

        }
    }
}
