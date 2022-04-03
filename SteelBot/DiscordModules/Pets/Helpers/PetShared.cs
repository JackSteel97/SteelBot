using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using SteelBot.Database.Models;
using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Generation;
using SteelBot.DiscordModules.Pets.Models;
using SteelBot.Helpers;
using SteelBot.Helpers.Constants;
using SteelBot.Helpers.Extensions;
using SteelBot.Helpers.Levelling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Helpers
{
    public static class PetShared
    {
        private const int NewPetSlotUnlockLevels = 20;

        public static DiscordEmbedBuilder GetOwnedPetsBaseEmbed(User user, List<Pet> availablePets, List<Pet> disabledPets, string username = "Your")
        {
            var embedBuilder = new DiscordEmbedBuilder()
                .WithColor(EmbedGenerator.InfoColour)
                .WithTitle($"{username} Owned Pets");

            if (disabledPets.Count > 0)
            {
                embedBuilder.WithFooter("Inactive pet's bonuses have no effect until you reach the required level in this server or activate bonus pet slots.");
            }

            var bonusCapacity = GetBonusValue(availablePets, BonusType.PetSlots);
            var petCapacity = GetPetCapacity(user, bonusCapacity);
            var ownedPets = availablePets.Count + disabledPets.Count;
            embedBuilder
                .AddField("Pet Slots", $"{ownedPets} / {petCapacity}")
                .WithTimestamp(DateTime.Now);

            return embedBuilder;
        }

        public static StringBuilder AppendPetDisplayShort(StringBuilder builder, Pet pet, bool active, double baseCapacity, double maxCapacity)
        {
            builder.AppendLine(Formatter.Bold(pet.GetName()))
                .Append("Level ").Append(pet.CurrentLevel).Append(' ').Append(Formatter.Italic(pet.Rarity.ToString())).Append(' ').Append(pet.Species.GetName());

            if (!active)
            {
                var levelRequired = GetRequiredLevelForPet(pet.Priority, baseCapacity, maxCapacity);
                builder.Append(" - **Inactive**, Level ").Append(levelRequired).Append(" required");
            }

            return builder.AppendLine()
                .AppendLine(GetPetLevelProgressBar(pet));
        }

        public static List<Pet> GetAvailablePets(User user, List<Pet> allPets, out List<Pet> disabledPets)
        {
            int capacity = GetPetCapacity(user, 0);
            var availablePets = new List<Pet>();
            if (allPets.Count > 0)
            {
                var orderedPets = allPets.OrderBy(p => p.Priority).ToList();

                int currentIndex = 0;
                while (availablePets.Count < capacity && currentIndex < orderedPets.Count)
                {
                    availablePets.Add(orderedPets[currentIndex]);
                    ++currentIndex;
                    capacity = GetPetCapacity(user, GetBonusValue(availablePets, BonusType.PetSlots));
                }
                var availableCount = Math.Max(1, Math.Min(capacity, orderedPets.Count));
                if (availableCount != availablePets.Count)
                {
                    availablePets = availablePets.Take(availableCount).ToList();
                }
                disabledPets = orderedPets.Skip(availableCount).ToList();
            }
            else
            {
                disabledPets = new List<Pet>();
            }
            return availablePets;
        }

        public static int GetPetCapacityFromAllPets(User user, List<Pet> allPets)
        {
            var activePets = GetAvailablePets(user, allPets, out _);
            return GetPetCapacity(user, activePets);
        }

        public static int GetPetCapacity(User user, List<Pet> activePets)
        {
            var bonusCapacity = GetBonusValue(activePets, BonusType.PetSlots);
            return GetPetCapacity(user, bonusCapacity);
        }

        public static int GetPetCapacity(User user, double bonusCapacity)
        {
            // Restrict pet bonus slots to +50.
            int result = 1 + Math.Min(50, Convert.ToInt32(Math.Floor(bonusCapacity)));

            if (user != default)
            {
                result += (user.CurrentLevel / NewPetSlotUnlockLevels);
            }
            return Math.Max(result, 1);
        }

        public static bool TryGetPetIdFromComponentId(string buttonId, out long petId)
        {
            var parts = buttonId.Split(':');
            if (parts.Length == 2)
            {
                return long.TryParse(parts[1], out petId);
            }
            petId = default;
            return false;
        }

        public static bool PetXpChanged(Pet pet, StringBuilder changes, out bool shouldPingOwner)
        {
            shouldPingOwner = false;
            var newBonuses = new List<PetBonus>();
            bool levelledUp = LevellingMaths.UpdatePetLevel(pet.CurrentLevel, pet.EarnedXp, pet.Rarity, out var newLevel);
            if (levelledUp)
            {
                changes.Append("Your pet ").Append(Formatter.Italic(pet.GetName())).Append(" advanced to level ").Append(Formatter.Bold(newLevel.ToString())).AppendLine(" and improved their abilities!");
                int nextLevel = pet.CurrentLevel + 1;
                pet.CurrentLevel = newLevel;
                for (int level = nextLevel; level <= pet.CurrentLevel; ++level)
                {
                    var newBonus = PetLevelledUp(pet, level);
                    if (newBonus != null)
                    {
                        newBonuses.Add(newBonus);
                    }
                }
            }

            if (newBonuses.Count > 0)
            {
                shouldPingOwner = true;
                changes.AppendLine(Formatter.Bold("Learned New Bonuses:"));
                foreach (var newBonus in newBonuses)
                {
                    PetDisplayHelpers.AppendBonusDisplay(changes, newBonus);
                }
            }

            return levelledUp;
        }

        public static int GetDisconnectedXpPerMin(List<Pet> availablePets)
        {
            int disconnectedXpPerMin = 0;
            foreach (var pet in availablePets)
            {
                if (pet.Rarity == Rarity.Legendary)
                {
                    // Legendary pets earn passive xp.
                    disconnectedXpPerMin += pet.CurrentLevel;
                }
                else if (pet.Rarity == Rarity.Mythical)
                {
                    disconnectedXpPerMin += pet.CurrentLevel * 2;
                }
            }
            return disconnectedXpPerMin;
        }

        public static async Task SendPetLevelledUpMessage(StringBuilder changes, Guild guild, DiscordGuild discordGuild, ulong userId, bool pingUser)
        {
            var channel = guild.GetLevelAnnouncementChannel(discordGuild);
            if (channel != null)
            {
                var message = new DiscordMessageBuilder();

                if (pingUser)
                {
                    message = message.WithContent(userId.ToMention());
                }
                else
                {
                    changes.Insert(0, userId.ToMention() + Environment.NewLine);
                }

                message = message.WithEmbed(EmbedGenerator.Info(changes.ToString(),
                            "Pet Level Up"));

                await channel.SendMessageAsync(message);
            }
        }

        public static string GetPetLevelProgressBar(Pet pet)
        {
            const string progressCharacter = EmojiConstants.Symbols.GreenSquare;
            const string remainingCharacter = EmojiConstants.Symbols.GreySquare;

            var thisLevelXp = LevellingMaths.PetXpForLevel(pet.CurrentLevel, pet.Rarity);
            var nextLevelXp = LevellingMaths.PetXpForLevel(pet.CurrentLevel + 1, pet.Rarity);
            var xpIntoThisLevel = pet.EarnedXp - thisLevelXp;
            var xpToLevelUp = nextLevelXp - thisLevelXp;

            var progress = Math.Min(Math.Max(0, xpIntoThisLevel / xpToLevelUp), 1);
            const int totalCharacterCount = 10;
            int progressedCharacterCount = Convert.ToInt32(totalCharacterCount * progress);
            int remainingCharacters = totalCharacterCount - progressedCharacterCount;

            var sb = new StringBuilder();
            sb.Append('[');
            if (progressedCharacterCount > 0)
            {
                for (int i = 0; i < progressedCharacterCount; ++i)
                {
                    sb.Append(progressCharacter);
                }
            }

            if (remainingCharacters > 0)
            {
                for (int i = 0; i < remainingCharacters; ++i)
                {
                    sb.Append(remainingCharacter);
                }
            }
            sb.Append(']').Append(" Level ").Append(pet.CurrentLevel + 1);
            return sb.ToString();
        }

        public static double GetBonusValue(List<Pet> activePets, BonusType targetType)
        {
            double multiplier = targetType.IsPercentage() ? 1 : 0;
            foreach (var pet in activePets)
            {
                foreach (var bonus in pet.Bonuses)
                {
                    if (bonus.BonusType.HasFlag(targetType))
                    {
                        multiplier += bonus.Value;
                    }
                }
            }

            return multiplier;
        }

        public static List<PetWithActivation> Recombine(List<Pet> availablePets, List<Pet> disabledPets)
        {
            var combinedPets = new List<PetWithActivation>(availablePets.Count + disabledPets.Count);
            if (availablePets.Count > 0)
            {
                combinedPets.AddRange(availablePets.ConvertAll(p => new PetWithActivation(p, true)));
            }
            if (disabledPets.Count > 0)
            {
                combinedPets.AddRange(disabledPets.ConvertAll(p => new PetWithActivation(p, false)));
            }
            return combinedPets;
        }

        public static int GetRequiredLevelForPet(int petPriority, double baseCapacity, double totalCapacity)
        {
            var currentLevelBracket = (baseCapacity - 1) * NewPetSlotUnlockLevels;
            var extraRequiredLevels = ((petPriority + 1) - totalCapacity) * NewPetSlotUnlockLevels;
            return Convert.ToInt32(currentLevelBracket + extraRequiredLevels);
        }

        private static PetBonus PetLevelledUp(Pet pet, int level)
        {
            PetBonus newBonus = null;
            if (level == 10 || level % 25 == 0)
            {
                // New bonuses gained at level 10, 25, 50, 75, etc...
                newBonus = GivePetNewBonus(pet);
            }
            ImproveCurrentPetBonuses(pet);
            return newBonus;
        }

        private static PetBonus GivePetNewBonus(Pet pet)
        {
            var bonus = PetFactory.GenerateBonus(pet);
            pet.AddBonus(bonus);
            return bonus;
        }

        private static void ImproveCurrentPetBonuses(Pet pet)
        {
            foreach (var bonus in pet.Bonuses)
            {
                var increase = Math.Abs(bonus.Value * 0.01);
                bonus.Value += increase;
            }
        }
    }
}
