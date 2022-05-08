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
            var size = GetRandomEnumValue<Size>();
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

            var bonuses = BuildBonuses(pet, levelOfUser);
            pet.AddBonuses(bonuses);

            Logger.LogDebug("Generated a new pet with Base Rarity {BaseRarity} and Final Rarity {FinalRarity}", baseRarity.ToString(), finalRarity.ToString());
            return pet;
        }

        private static Rarity GetBaseRarity()
        {
            const int maxBound = 10000;
            const double MythicalChance = 0.0001;
            const double LegendaryChance = 0.01;
            const double EpicChance = 0.10;
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
                    Name = part.Humanize(),
                    Description = GenerateColourCombo()
                };
                attributes.Add(attribute);
            }
            return attributes;
        }

        public static PetBonus GenerateBonus(Pet pet, int levelOfUser, List<PetBonus> existingBonuses = default)
        {
            existingBonuses ??= pet.Bonuses;
            bool validBonus = true;
            var maxPercentageBonus = pet.Rarity.GetMaxBonusValue();
            PetBonus bonus;
            do
            {
                bonus = new PetBonus()
                {
                    Pet = pet,
                    BonusType = GetWeightedRandomBonusType(pet.Rarity)
                };

                if (bonus.BonusType.IsPercentage())
                {
                    validBonus = HandlePercentageBonusGeneration(pet, maxPercentageBonus, bonus, existingBonuses);
                }
                else if(bonus.BonusType == BonusType.OfflineXP)
                {
                    HandleOfflineXpBonusGeneration(bonus, pet.Rarity, pet.CurrentLevel, levelOfUser);
                }
                else
                {
                    HandleIntegerBonusGeneration(bonus, pet.Rarity);
                }
            } while (!validBonus);

            return bonus;
        }

        private static BonusType GetWeightedRandomBonusType(Rarity rarity)
        {
            var excludedTypes = new List<BonusType> { BonusType.None };
            double rarityValue = (double)rarity;
            double chanceToGeneratePassiveXp = rarityValue / 10;
            double chanceToGeneratePetSlots = 0.2 + (rarityValue / 10);

            if (MathsHelper.TrueWithProbability(1 - chanceToGeneratePassiveXp))
            {
                excludedTypes.Add(BonusType.OfflineXP);
            }
            if (MathsHelper.TrueWithProbability(1 - chanceToGeneratePetSlots))
            {
                excludedTypes.Add(BonusType.PetSlots);
            }

            return GetRandomEnumValue<BonusType>(excludedTypes.ToArray());
        }

        private static void HandleOfflineXpBonusGeneration(PetBonus bonus, Rarity rarity, int petLevel, int userLevel)
        {
            double baseValue = (double)rarity;

            double chanceToGetMore = baseValue / 10;

            double petLevelMultiplier = 1 + ((double)petLevel / 100);
            double userLevelMultiplier = 1 + ((double)userLevel / 100);
            baseValue *= petLevelMultiplier;
            baseValue *= userLevelMultiplier;
            bonus.Value = baseValue;

            while (MathsHelper.TrueWithProbability(chanceToGetMore))
            {
                bonus.Value += baseValue;
            }
        }

        private static void HandleIntegerBonusGeneration(PetBonus bonus, Rarity rarity)
        {
            bonus.Value = 1;
            while (MathsHelper.TrueWithProbability(0.1))
            {
                ++bonus.Value;
            }

            var probabilityToGoNegative = ((double)rarity + 1) / 10;
            if (MathsHelper.TrueWithProbability(probabilityToGoNegative))
            {
                bonus.Value *= -1;
            }
        }

        private static bool HandlePercentageBonusGeneration(Pet pet, double maxBonus, PetBonus bonus, List<PetBonus> existingBonuses)
        {
            bool validBonus = true;
            var minBonus = pet.Rarity < Rarity.Rare && !bonus.BonusType.IsNegative() ? 0 : maxBonus * -1; // Lower rarities shouldn't have negative bonuses.

            bonus.Value = GetRandomPercentageBonus(maxBonus, minBonus);
            if (bonus.Value < 0 && !bonus.BonusType.IsNegative())
            {
                // Normally positive bonuses being negative should be less common.
                if (MathsHelper.TrueWithProbability(0.8))
                {
                    bonus.Value *= -1;
                }
            }

            // Check this won't cause negative bonuses to go far
            if (bonus.Value < 0 && existingBonuses?.Count > 0)
            {
                var currentTotal = existingBonuses.Where(p => p.BonusType == bonus.BonusType).Sum(x => x.Value);
                var newTotal = currentTotal + bonus.Value;
                if (newTotal < -1)
                {
                    validBonus = false;
                }
            }

            return validBonus;
        }

        private static List<PetBonus> BuildBonuses(Pet pet, int levelOfUser)
        {
            var maxBonuses = pet.Rarity.GetStartingBonusCount();
            var bonuses = new List<PetBonus>(maxBonuses);

            for (int i = 0; i < maxBonuses; ++i)
            {
                var bonus = GenerateBonus(pet, levelOfUser, bonuses);
                bonuses.Add(bonus);
            }
            return bonuses;
        }

        private static string GenerateColourCombo()
        {
            if (MathsHelper.TrueWithProbability(0.1))
            {
                // Has two colours.
                var primary = GetRandomEnumValue<Colour>();
                var secondary = GetRandomEnumValue<Colour>();
                var mixing = GetRandomEnumValue<ColourMixing>();

                return $"{primary.Humanize()} and {secondary.Humanize()} {mixing.Humanize()} patterned";
            }
            else
            {
                // Has one colour.
                var colour = GetRandomEnumValue<Colour>();
                return colour.Humanize();
            }
        }

        private static T GetRandomEnumValue<T>(params T[] excluding)
        {
            T result;
            var excludedValues = excluding.ToHashSet();
            do
            {
                var values = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
                result = values[RandomNumberGenerator.GetInt32(values.Length)];
            } while (excludedValues.Contains(result));
            return result;
        }

        private static double GetRandomPercentageBonus(double maxValue = 1, double minValue = -1)
        {
            var random = GetRandomDouble();
            return minValue + (random * (maxValue - minValue));
        }

        private static double GetRandomDouble()
        {
            const int maxValue = 1001;
            const double maxDoubleVal = maxValue;
            return RandomNumberGenerator.GetInt32(1, maxValue) / maxDoubleVal;
        }
    }
}
