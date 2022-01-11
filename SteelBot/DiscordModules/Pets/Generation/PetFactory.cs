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
            var species = Enum.GetValues(typeof(Species)).Cast<Species>().ToList();
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
            var size = GetSize();
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

        private static Size GetSize()
        {
            var sizes = Enum.GetValues(typeof(Size)).Cast<Size>().ToList();
            return sizes[RandomNumberGenerator.GetInt32(sizes.Count)];
        }
    }
}
