﻿using Humanizer;
using Microsoft.Extensions.Logging;
using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.Helpers;
using SteelBot.Helpers.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace SteelBot.DiscordModules.Pets.Generation
{
    public class PetFactory
    {
        private readonly ILogger<PetFactory> Logger;

        private readonly Dictionary<Rarity, List<Species>> SpeciesByRarity = new();

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

        public ulong CountPossibilities()
        {
            ulong possibilities = 0;
            var colours = Enum.GetValues(typeof(Colour)).Cast<Colour>().ToArray();
            var colourMixings = Enum.GetValues(typeof(ColourMixing)).Cast<ColourMixing>().ToArray();
            var bonuses = Enum.GetValues(typeof(BonusType)).Cast<BonusType>().ToArray();
            var sizes = Enum.GetValues(typeof(Size)).Cast<Size>().ToArray();

            foreach (var speciesGrouping in SpeciesByRarity)
            {
                var startingBonuses = speciesGrouping.Key.GetStartingBonusCount();

                foreach (var species in speciesGrouping.Value)
                {
                    foreach (var size in sizes)
                    {
                        var bonusMultiplier = PermutationsAndCombinations.nCr(bonuses.Length, startingBonuses);
                        for (int i = 0; i < bonusMultiplier; ++i)
                        {
                            foreach (var bodyPart in species.GetBodyParts())
                            {
                                foreach (var colour in colours)
                                {
                                    ++possibilities;
                                }
                                foreach (var mixing in colourMixings)
                                {
                                    foreach (var colour in colours)
                                    {
                                        foreach (var colour2 in colours)
                                        {
                                            ++possibilities;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return possibilities;
        }

        public Pet Generate(int levelOfUser = 0)
        {
            var baseRarity = GetBaseRarity();
            var species = GetSpecies(baseRarity);
            var finalRarity = GetFinalRarity(baseRarity);
            var size = PetGenerationShared.GetRandomEnumValue<Size>();
            var birthDate = GetBirthDate(species);

            var pet = new Pet()
            {
                Rarity = finalRarity,
                Species = species,
                Size = size,
                BornAt = birthDate,
                FoundAt = DateTime.UtcNow
            };

            pet.Attributes = BuildAttributes(pet);

            var bonuses = PetBonusFactory.GenerateMany(pet, levelOfUser);
            pet.AddBonuses(bonuses);

            Logger.LogDebug("Generated a new pet with Base Rarity {BaseRarity} and Final Rarity {FinalRarity}", baseRarity.ToString(), finalRarity.ToString());
            return pet;
        }

        private static Rarity GetBaseRarity()
        {
            const int maxBound = 100000;
            const double MythicalChance = 0.0001;
            const double LegendaryChance = 0.005;
            const double EpicChance = 0.06;
            const double RareChance = 0.20;
            const double UncommonChance = 0.50;

            const double MythicalBound = maxBound * MythicalChance;
            const double LegendaryBound = maxBound * LegendaryChance;
            const double EpicBound = maxBound * EpicChance;
            const double RareBound = maxBound * RareChance;
            const double UncommonBound = maxBound * UncommonChance;

            int random = RandomNumberGenerator.GetInt32(maxBound);

            if (random <= MythicalBound)
            {
                return Rarity.Mythical;
            }
            else if (random <= LegendaryBound)
            {
                return Rarity.Legendary;
            }
            else if (random <= EpicBound)
            {
                return Rarity.Epic;
            }
            else if (random <= RareBound)
            {
                return Rarity.Rare;
            }
            else if (random <= UncommonBound)
            {
                return Rarity.Uncommon;
            }
            else
            {
                return Rarity.Common;
            }
        }

        private static Rarity GetFinalRarity(Rarity rarity)
        {
            const double rarityUpChance = 0.1;
            const int maxRarity = (int)Rarity.Mythical;
            int currentRarity = (int)rarity;

            int finalRarity = currentRarity;

            for (int i = currentRarity + 1; i <= maxRarity; ++i)
            {
                if (!MathsHelper.TrueWithProbability(rarityUpChance))
                {
                    break;
                }
                finalRarity = i;
            }
            return (Rarity)finalRarity;
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
                    Name = part.ToString(),
                    Description = GenerateColourCombo()
                };
                attributes.Add(attribute);
            }
            return attributes;
        }

        private static string GenerateColourCombo()
        {
            if (MathsHelper.TrueWithProbability(0.1))
            {
                // Has two colours.
                var primary = PetGenerationShared.GetRandomEnumValue<Colour>();
                var secondary = PetGenerationShared.GetRandomEnumValue<Colour>();
                var mixing = PetGenerationShared.GetRandomEnumValue<ColourMixing>();

                return $"{primary} and {secondary} {mixing} patterned";
            }
            else
            {
                // Has one colour.
                var colour = PetGenerationShared.GetRandomEnumValue<Colour>();
                return colour.ToString();
            }
        }
    }
}
