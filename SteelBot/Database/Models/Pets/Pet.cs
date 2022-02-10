using Humanizer;
using SteelBot.DiscordModules.Pets.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.Database.Models.Pets
{
    public class Pet
    {
        public long RowId { get; set; }
        public ulong OwnerDiscordId { get; set; }
        [MaxLength(255)]
        public string Name { get; set; }

        public int Priority { get; set; }
        public double EarnedXp { get; set; }
        public int CurrentLevel { get; set; } = 1;
        public DateTime BornAt { get; set; }
        public DateTime FoundAt { get; set; }
        public Species Species { get; set; }
        public Size Size { get; set; }
        public Rarity Rarity { get; set; }
        public List<PetAttribute> Attributes { get; set; }
        public List<PetBonus> Bonuses { get; set; }

        public override string ToString()
        {
            TimeSpan age = DateTime.UtcNow - BornAt;

            return $"A {Rarity} {age.Humanize(maxUnit: Humanizer.Localisation.TimeUnit.Year)} old, {Size} {Species.GetName()} with\n\t{string.Join("\n\t", Attributes.Select(a => $"{a.Description} {a.Name}"))}";
        }

        public string GetName()
        {
            return Name ?? $"Unnamed {Species.GetName()}";
        }

        public bool IsPrimary => Priority == 0;
    }
}
