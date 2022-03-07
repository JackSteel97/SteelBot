using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using SteelBot.Database.Models;
using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Generation;
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
        public static DiscordEmbedBuilder GetOwnedPetsDisplayEmbed(User user, List<Pet> allPets, string username = "Your")
        {
            var embedBuilder = new DiscordEmbedBuilder()
                .WithColor(EmbedGenerator.InfoColour)
                .WithTitle($"{username} Owned Pets");

            var availablePets = GetAvailablePets(user, allPets, out var disabledPets);
            if (disabledPets.Count > 0)
            {
                embedBuilder.WithFooter("Inactive pet's bonuses have no effect until you reach the required level in this server.");
            }

            var petList = new StringBuilder();
            if (availablePets.Count > 0 || disabledPets.Count > 0)
            {
                foreach (var pet in availablePets)
                {
                    embedBuilder.AddField(pet.GetName(), $"Level {pet.CurrentLevel} {pet.Species.GetName()}{Environment.NewLine}{GetPetLevelProgressBar(pet)}");
                }

                int petNumber = availablePets.Count;
                foreach (var disabledPet in disabledPets)
                {
                    int levelRequired = GetRequiredLevelForPet(petNumber);
                    embedBuilder.AddField(disabledPet.GetName(), $"Level {disabledPet.CurrentLevel} {disabledPet.Species.GetName()} - **Inactive**, Level {levelRequired} required");
                    ++petNumber;
                }
            }
            else
            {
                petList.AppendLine("You currently own no pets.");
            }

            embedBuilder.WithDescription(petList.ToString());

            var bonusCapacity = GetBonusValue(availablePets, BonusType.PetSlots);
            var petCapacity = GetPetCapacity(user, bonusCapacity);
            var ownedPets = availablePets.Count + disabledPets.Count;
            embedBuilder
                .AddField("Pet Slots", $"{ownedPets} / {petCapacity}")
                .WithTimestamp(DateTime.Now);

            return embedBuilder;
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
                var availableCount = Math.Min(capacity, orderedPets.Count);
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
            int result = 1 + Convert.ToInt32(Math.Floor(bonusCapacity));

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

        public static bool PetXpChanged(Pet pet)
        {
            bool levelledUp = LevellingMaths.UpdatePetLevel(pet.CurrentLevel, pet.EarnedXp, pet.Rarity, out var newLevel);
            if (levelledUp)
            {
                int nextLevel = pet.CurrentLevel + 1;
                pet.CurrentLevel = newLevel;
                for (int level = nextLevel; level <= pet.CurrentLevel; ++level)
                {
                    PetLevelledUp(pet, level);
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

        public static async Task SendPetLevelledUpMessage(Pet pet, Guild guild, DiscordGuild discordGuild)
        {
            var channel = guild.GetLevelAnnouncementChannel(discordGuild);
            if (channel != null)
            {
                var message = new DiscordMessageBuilder()
                    .WithContent(pet.OwnerDiscordId.ToMention())
                    .WithEmbed(EmbedGenerator.Info($"Your pet {Formatter.Italic(pet.GetName())} advanced to level {Formatter.Bold(pet.CurrentLevel.ToString())} and improved their abilities!",
                    "Pet Level Up",
                    "Use the Pet Bonuses command to view their improved bonuses"));

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

        private static int GetRequiredLevelForPet(int petNumber)
        {
            return petNumber * NewPetSlotUnlockLevels;
        }

        private static void PetLevelledUp(Pet pet, int level)
        {
            if (level == 10 || level % 25 == 0)
            {
                // New bonuses gained at level 10, 25, 50, 75, etc...
                GivePetNewBonus(pet);
            }
            ImproveCurrentPetBonuses(pet);
        }

        private static void GivePetNewBonus(Pet pet)
        {
            var bonus = PetFactory.GenerateBonus(pet);
            pet.AddBonus(bonus);
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
