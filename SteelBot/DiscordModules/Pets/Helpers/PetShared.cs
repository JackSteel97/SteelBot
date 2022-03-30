﻿using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
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

        public static StringBuilder AppendPetDisplayShort(StringBuilder builder, Pet pet, bool active, double bonusCapacity)
        {
            builder.AppendLine(Formatter.Bold(pet.GetName()))
                .Append("Level ").Append(pet.CurrentLevel).Append(' ').Append(Formatter.Italic(pet.Rarity.ToString())).Append(' ').Append(pet.Species.GetName());

            if (!active)
            {
                var levelRequired = GetRequiredLevelForPet(pet.Priority, bonusCapacity);
                builder.Append(" - **Inactive**, Level ").Append(levelRequired).Append(" required");
            }

            return builder.AppendLine()
                .AppendLine(GetPetLevelProgressBar(pet));
        }

        public static List<Pet> GetAvailablePets(User user, List<Pet> allPets, out List<Pet> disabledPets)
        {
            int baseCapacity = GetPetCapacity(user, 0);
            int capacity = baseCapacity;
            var availablePets = new List<Pet>();
            if (allPets.Count > 0)
            {
                var orderedPets = allPets.OrderBy(p => p.Priority).ToList();

                int currentIndex = 0;
                while (availablePets.Count < capacity && currentIndex < orderedPets.Count)
                {
                    availablePets.Add(orderedPets[currentIndex]);
                    ++currentIndex;
                    capacity = baseCapacity + Convert.ToInt32(GetBonusValue(availablePets, BonusType.PetSlots));
                }
                var availableCount = Math.Max(1, Math.Min(capacity, orderedPets.Count));
                disabledPets = orderedPets.Skip(availableCount).ToList();
            }
            else
            {
                disabledPets = new List<Pet>();
            }
            return availablePets;
        }

        public static int GetPetCapacity(User user, double bonusCapacity)
        {
            // Restrict pet bonus slots to +50.
            int result = 1 + Math.Min(50, Convert.ToInt32(Math.Floor(bonusCapacity)));

            if (user != default)
            {
                result += (user.CurrentLevel / NewPetSlotUnlockLevels);
            }
            return Math.Max(result, 0);
        }

        public static bool TryGetPetIdFromPetSelectorButton(string buttonId, out long petId)
        {
            var parts = buttonId.Split('_');
            if (parts.Length == 3)
            {
                return long.TryParse(parts[2], out petId);
            }
            petId = default;
            return false;
        }

        public static bool PetXpChanged(Pet pet, StringBuilder changes)
        {
            List<PetBonus> newBonuses = new List<PetBonus>();
            bool levelledUp = LevellingMaths.UpdatePetLevel(pet.CurrentLevel, pet.EarnedXp, pet.Rarity, out var newLevel);
            if (levelledUp)
            {
                changes.Append("Your pet ").Append(Formatter.Italic(pet.GetName())).Append(" advanced to level ").Append(Formatter.Bold(newLevel.ToString())).AppendLine(" and improved their abilities!");
                int nextLevel = pet.CurrentLevel + 1;
                pet.CurrentLevel = newLevel;
                for (int level = nextLevel; level <= pet.CurrentLevel; ++level)
                {
                    var newBonus = PetLevelledUp(pet, level);
                    if (newBonus != null) {
                        newBonuses.Add(newBonus);
                    }
                }
            }

            if(newBonuses.Count > 0)
            {
                changes.AppendLine(Formatter.Bold("Learned New Bonuses:"));
                foreach(var newBonus in newBonuses)
                {
                   PetDisplayHelpers.AppendBonusDisplay(changes, newBonus);
                }
            }

            return levelledUp;
        }

        public static async Task<(bool nameChanged, ulong nameMessageId)> HandleNamingPet(CommandContext context, Pet pet)
        {
            bool validName = false;
            bool nameChanged = false;
            ulong nameMessageId = 0;
            while (!validName)
            {
                DiscordMessage nextMessage = null;
                var nameResult = await context.Message.GetNextMessageAsync(m =>
                {
                    nextMessage = m;
                    nameMessageId = m.Id;
                    return true;
                });

                if (!nameResult.TimedOut)
                {
                    validName = await ValidateAndName(pet, nextMessage);
                }
                else
                {
                    await context.Channel.SendMessageAsync(PetMessages.GetNamingTimedOutMessage(pet).WithReply(context.Message.Id, mention: true));
                    validName = true;
                }
                if (!validName)
                {
                    await nextMessage.RespondAsync(EmbedGenerator.Primary($"What would you like to name your {pet.Species.GetName()} instead?", "Ok, try again"));
                }
                else
                {
                    nameChanged = true;
                }
            }
            return (nameChanged, nameMessageId);
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

        public static async Task SendPetLevelledUpMessage(StringBuilder changes, Guild guild, DiscordGuild discordGuild, ulong userId)
        {
            var channel = guild.GetLevelAnnouncementChannel(discordGuild);
            if (channel != null)
            {
                var message = new DiscordMessageBuilder()
                    .WithContent(userId.ToMention())
                    .WithEmbed(EmbedGenerator.Info(changes.ToString(),
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
                for(int i = 0; i < progressedCharacterCount; ++i)
                {
                    sb.Append(progressCharacter);
                }
            }

            if(remainingCharacters > 0)
            {
                for(int i =0; i< remainingCharacters; ++i)
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
            List<PetWithActivation> combinedPets = new List<PetWithActivation>(availablePets.Count + disabledPets.Count);
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

        private static async Task<bool> ValidateAndName(Pet pet, DiscordMessage nameMessage)
        {
            bool named = false;
            if (nameMessage.Content.Length > 70)
            {
                await nameMessage.RespondAsync(EmbedGenerator.Warning("Sorry, that name is too long. Please try something else"));
            }
            else if (await ConfirmNaming(pet, nameMessage))
            {
                pet.Name = nameMessage.Content;
                named = true;
            }

            return named;
        }

        private static async Task<bool> ConfirmNaming(Pet pet, DiscordMessage nameMessage)
        {
            var confirmationEmbed = EmbedGenerator.Info($"Ok, you'd like to name this {pet.Species.GetName()} \"{Formatter.Italic(nameMessage.Content)}\"?", "Are you sure?");
            var confirmationResponseBuilder = new DiscordMessageBuilder()
                .WithEmbed(confirmationEmbed)
                .AddComponents(Interactions.Pets.NameConfirm, Interactions.Pets.NameRetry);

            var confirmationMessage = await nameMessage.RespondAsync(confirmationResponseBuilder);
            var confirmResult = await confirmationMessage.WaitForButtonAsync(nameMessage.Author);

            if (!confirmResult.TimedOut)
            {
                confirmationResponseBuilder.ClearComponents();
                await confirmResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(confirmationResponseBuilder));
                return confirmResult.Result.Id == InteractionIds.Pets.NameConfirm;
            }
            else
            {
                await confirmationMessage.DeleteAsync();
                await nameMessage.RespondAsync(PetMessages.GetNamingTimedOutMessage(pet));
                return true;
            }
        }

        public static int GetRequiredLevelForPet(int petPriority, double bonusCapacity)
        {
            return (petPriority - (int)bonusCapacity) * NewPetSlotUnlockLevels;
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
