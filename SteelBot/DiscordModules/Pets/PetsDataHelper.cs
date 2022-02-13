﻿using DSharpPlus.Entities;
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
using SteelBot.DiscordModules.Pets.Generation;
using DSharpPlus.CommandsNext;
using SteelBot.Helpers.Constants;
using DSharpPlus.Interactivity.Extensions;
using System.Security.Cryptography;
using SteelBot.Helpers.Levelling;

namespace SteelBot.DiscordModules.Pets
{
    public class PetsDataHelper
    {
        private readonly DataCache Cache;
        private readonly AppConfigurationService AppConfigurationService;
        private readonly PetFactory PetFactory;
        private readonly ILogger<PetsDataHelper> Logger;
        private const int NewPetSlotUnlockLevels = 20;

        public PetsDataHelper(DataCache cache, AppConfigurationService appConfigurationService, PetFactory petFactory, ILogger<PetsDataHelper> logger)
        {
            Cache = cache;
            AppConfigurationService = appConfigurationService;
            PetFactory = petFactory;
            Logger = logger;
        }
        public DiscordMessageBuilder GetPetOwnedSuccessMessage(DiscordMember owner, Pet pet)
        {
            var nameInsert = !string.IsNullOrWhiteSpace(pet.Name) ? Formatter.Italic(pet.Name) : "";
            var embedBuilder = new DiscordEmbedBuilder()
               .WithColor(new DiscordColor(pet.Rarity.GetColour()))
               .WithTitle("Congrats")
               .WithDescription($"{owner.Mention} Congratulations on your new pet {nameInsert}");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }

        public DiscordMessageBuilder GetPetRanAwayMessage(Pet pet)
        {
            var embedBuilder = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(pet.Rarity.GetColour()))
                .WithTitle("It got away!")
                .WithDescription($"The {pet.Species.GetName()} ran away before you could befriend it.{Environment.NewLine}Better luck next time!");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }

        public DiscordMessageBuilder GetBefriendFailedMessage(Pet pet)
        {
            var embedBuilder = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(pet.Rarity.GetColour()))
                .WithTitle("Failed to befriend!")
                .WithDescription($"The {pet.Species.GetName()} ran away as soon as you moved closer.{Environment.NewLine}Better luck next time!");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }

        public DiscordMessageBuilder GetBefriendSuccessMessage(Pet pet)
        {
            var embedBuilder = EmbedGenerator.Success("What would you like to name it?", $"You befriended the {pet.Species.GetName()}!");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }

        public DiscordMessageBuilder GetNamingTimedOutMessage(Pet pet)
        {
            var embedBuilder = EmbedGenerator.Info($"You can give your pet {pet.Species.GetName()} a name later instead.", $"Looks like you're busy");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }

        public DiscordEmbedBuilder GetPetDisplayEmbed(Pet pet, bool includeName = true)
        {
            string name;
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

            foreach (var attribute in pet.Attributes)
            {
                embedBuilder.AddField(attribute.Name, Formatter.InlineCode(attribute.Description), true);
            }

            StringBuilder bonuses = new StringBuilder();
            foreach (var bonus in pet.Bonuses)
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

        public DiscordEmbedBuilder GetOwnedPetsDisplayEmbed(ulong guildId, ulong userId)
        {
            var embedBuilder = new DiscordEmbedBuilder()
                .WithColor(EmbedGenerator.InfoColour)
                .WithTitle("Your Pets");

            var availablePets = GetAvailablePets(guildId, userId, out var disabledPets);
            if (disabledPets.Count > 0)
            {
                embedBuilder.WithFooter("Disabled pet's bonuses have no effect until you reach the required level in this server.");
            }

            StringBuilder petList = new StringBuilder();
            if (availablePets.Count > 0 || disabledPets.Count > 0)
            {
                foreach (var pet in availablePets)
                {
                    AppendShortDescription(petList, pet);
                    petList.AppendLine();
                }

                int petNumber = availablePets.Count;
                foreach (var disabledPet in disabledPets)
                {
                    AppendShortDescription(petList, disabledPet);
                    int levelRequired = GetRequiredLevelForPet(petNumber);
                    petList.Append(" - Disabled, Level ").Append(petNumber).Append(" required");
                    petList.AppendLine();
                    ++petNumber;
                }
            }
            else
            {
                petList.AppendLine("You currently own no pets.");
            }

            embedBuilder.WithDescription(petList.ToString());



            var petCapacity = GetPetCapacity(guildId, userId);
            var ownedPets = availablePets.Count + disabledPets.Count;
            embedBuilder
                .AddField("Pet Slots", $"{ownedPets} / {petCapacity}")
                .WithTimestamp(DateTime.UtcNow);

            return embedBuilder;
        }

        public bool SearchSuccess(DiscordMember userSearching)
        {
            var probability = GetSearchSuccessProbability(userSearching);
            return MathsHelper.TrueWithProbability(probability);
        }

        public bool BefriendSuccess(DiscordMember user, Pet target)
        {
            var probability = GetBefriendSuccessProbability(user, target);
            return MathsHelper.TrueWithProbability(probability);
        }

        public async Task<(bool befriendAttempt, Pet pet)> HandleInitialSearchSuccess(CommandContext context)
        {
            bool befriendAttempt = false;
            var hasSpace = HasSpaceForAnotherPet(context.Member);
            string noSpaceMessage = "";
            if (!hasSpace)
            {
                noSpaceMessage = " But you don't have enough room for another pet!";
            }

            var foundPet = PetFactory.Generate();
            var initialPetDisplay = GetPetDisplayEmbed(foundPet, includeName: false);

            var initialResponseBuilder = new DiscordMessageBuilder()
                .WithContent($"You found a new potential friend!{noSpaceMessage}")
                .WithEmbed(initialPetDisplay)
                .AddComponents(new DiscordComponent[] {
                    Interactions.Pets.Befriend.Disable(!hasSpace),
                    Interactions.Pets.Leave
                });

            var message = await context.RespondAsync(initialResponseBuilder);
            var result = await message.WaitForButtonAsync(context.Member);

            if (!result.TimedOut)
            {
                initialResponseBuilder.ClearComponents();
                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(initialResponseBuilder));
                befriendAttempt = result.Result.Id == InteractionIds.Pets.Befriend;
            }
            else
            {
                await message.DeleteAsync();
                await context.RespondAsync(GetPetRanAwayMessage(foundPet));
            }

            return (befriendAttempt, foundPet);
        }

        public async Task HandleManagePets(CommandContext context)
        {
            var petList = GetOwnedPetsDisplayEmbed(context.Guild.Id, context.Member.Id);

            var initialResponseBuilder = new DiscordMessageBuilder().WithEmbed(petList);


            if (Cache.Pets.TryGetUsersPets(context.Member.Id, out var allPets))
            {
                int columnCounter = 0;
                int rowCounter = 0;
                List<DiscordComponent> components = new List<DiscordComponent>();
                foreach (var pet in allPets)
                {
                    components.Add(Interactions.Pets.Manage(pet.RowId, pet.GetName()));
                    ++columnCounter;
                    if (columnCounter % 4 == 0)
                    {
                        // Start a new row.
                        initialResponseBuilder.AddComponents(components);
                        components.Clear();
                        columnCounter = 0;
                        ++rowCounter;
                        if (rowCounter == 5)
                        {
                            // Can't fit any more rows
                            break;
                        }
                    }
                }
                if(rowCounter < 5)
                {
                    initialResponseBuilder.AddComponents(components);
                }

                var message = await context.RespondAsync(initialResponseBuilder);
                var result = await message.WaitForButtonAsync(context.Member);

                initialResponseBuilder.ClearComponents();
                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(initialResponseBuilder));

                if (!result.TimedOut)
                {
                    // Figure out which pet they want to manage.
                    var parts = result.Result.Id.Split('_');
                    if (parts.Length == 3)
                    {
                        if (long.TryParse(parts[2], out long petId))
                        {
                            await HandleManagePet(context, petId);
                        }
                    }
                }
            }
        }

        public async Task HandleManagePet(CommandContext context, long petId)
        {
            if(Cache.Pets.TryGetPet(context.Member.Id, petId, out var pet))
            {
                Cache.Pets.TryGetUsersPetsCount(context.Member.Id, out var ownedPetCount);
                var petDisplay = GetPetDisplayEmbed(pet);
                var initialResponseBuilder = new DiscordMessageBuilder()
                    .WithEmbed(petDisplay)
                    .AddComponents(new DiscordComponent[]
                    {
                        Interactions.Pets.Rename,
                        Interactions.Pets.MakePrimary.Disable(pet.IsPrimary),
                        Interactions.Pets.IncreasePriority.Disable(pet.IsPrimary),
                        Interactions.Pets.DecreasePriority.Disable(pet.Priority == (ownedPetCount-1)),
                        Interactions.Pets.Abandon
                    });

                var message = await context.RespondAsync(initialResponseBuilder);
                var result = await message.WaitForButtonAsync(context.Member);

                initialResponseBuilder.ClearComponents();
                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(initialResponseBuilder));

                if (!result.TimedOut)
                {
                    switch (result.Result.Id)
                    {
                        case InteractionIds.Pets.Rename:
                            break;
                        case InteractionIds.Pets.MakePrimary:
                            break;
                        case InteractionIds.Pets.IncreasePriority:
                            break;
                        case InteractionIds.Pets.DecreasePriority:
                            break;
                        case InteractionIds.Pets.Abandon:
                            break;
                    }
                }
            }
            else
            {
                await context.RespondAsync(EmbedGenerator.Error("Something went wrong and I couldn't find that pet. Please try again later."));
            }
        }

        private async Task<bool> GetConfirmation(CommandContext context)
        {
            var confirmMessageBuilder = new DiscordMessageBuilder()
                .WithContent($"Attention {context.Member.Mention}")
                .WithEmbed(EmbedGenerator.Warning("This action cannot be undone, please confirm you want to continue."))
                .AddComponents(new DiscordComponent[] {
                    Interactions.Confirmation.Confirm,
                    Interactions.Confirmation.Cancel
                });

            var message = await context.Channel.SendMessageAsync(confirmMessageBuilder);

            var result = await message.WaitForButtonAsync(context.Member);
            return !result.TimedOut && result.Result.Id == InteractionIds.Confirmation.Confirm;
        }

        public async Task<bool> HandleBefriendAttempt(CommandContext context, Pet pet)
        {
            bool befriendSuccess = BefriendSuccess(context.Member, pet);
            if (befriendSuccess)
            {
                pet.OwnerDiscordId = context.Member.Id;
                await HandleBefriendSuccess(context, pet);
                await Cache.Pets.InsertPet(pet);
            }
            else
            {
                var response = GetBefriendFailedMessage(pet).WithReply(context.Message.Id, mention: true);
                await context.Channel.SendMessageAsync(response);
            }
            return befriendSuccess;
        }

        public async Task HandleBefriendSuccess(CommandContext context, Pet pet)
        {
            var successMessage = GetBefriendSuccessMessage(pet).WithReply(context.Message.Id, mention: true);
            await context.Channel.SendMessageAsync(successMessage);
            await HandleNamingPet(context, pet);
        }

        public async Task HandleNamingPet(CommandContext context, Pet pet)
        {
            bool named = false;
            while (!named)
            {
                DiscordMessage nextMessage = null;
                var nameResult = await context.Message.GetNextMessageAsync(m =>
                {
                    nextMessage = m;
                    return true;
                });

                if (!nameResult.TimedOut)
                {
                    named = await ValidateAndName(pet, nextMessage);
                }
                else
                {
                    await context.Channel.SendMessageAsync(GetNamingTimedOutMessage(pet).WithReply(context.Message.Id, mention: true));
                    named = true;
                }
                if (!named)
                {
                    await nextMessage.RespondAsync(EmbedGenerator.Primary($"What would you like to name your new {pet.Species.GetName()} instead?", "Ok, try again"));
                }
            }
        }

        public List<Pet> GetAvailablePets(ulong guildId, ulong userId, out List<Pet> disabledPets)
        {
            if (Cache.Pets.TryGetUsersPets(userId, out var pets))
            {
                var capacity = GetPetCapacity(guildId, userId);
                var availableCount = Math.Min(capacity, pets.Count);
                var orderedPets = pets.OrderBy(p => p.Priority);

                disabledPets = orderedPets.Skip(availableCount).ToList();
                return orderedPets.Take(availableCount).ToList();
            }
            else
            {
                disabledPets = new List<Pet>();
                return new List<Pet>();
            }
        }

        public List<Pet> GetAllPets(ulong userId)
        {
            if (Cache.Pets.TryGetUsersPets(userId, out var pets))
            {
                return pets.OrderBy(p => p.Priority).ToList();
            }
            else
            {
                return new List<Pet>();
            }
        }

        public async Task PetXpsUpdated(List<Pet> pets)
        {
            foreach (var pet in pets)
            {
                if (LevellingMaths.UpdateLevel(pet.CurrentLevel, pet.EarnedXp, out var newLevel))
                {
                    pet.CurrentLevel = newLevel;
                    PetLevelledUp(pet);
                }
                await Cache.Pets.UpdatePet(pet);
            }
        }

        private void AppendShortDescription(StringBuilder builder, Pet pet)
        {
            builder.Append(Formatter.Bold((pet.Priority + 1).ToString())).Append(" - Level ").Append(pet.CurrentLevel).Append(' ').Append(pet.Species.GetName()).Append(" *\"").Append(pet.GetName()).Append("\"*");
        }

        private void PetLevelledUp(Pet pet)
        {
            if (pet.CurrentLevel == 10 || pet.CurrentLevel % 25 == 0)
            {
                // New bonuses gained at level 10, 25, 50, 75, etc...
                GivePetNewBonus(pet);
            }
            ImproveCurrentPetBonuses(pet);
        }

        private void GivePetNewBonus(Pet pet)
        {
            var bonus = PetFactory.GenerateBonus(pet);
            pet.Bonuses.Add(bonus);
        }

        private void ImproveCurrentPetBonuses(Pet pet)
        {
            foreach (var bonus in pet.Bonuses)
            {
                bonus.PercentageValue *= 1.01;
            }
        }

        private async Task<bool> ValidateAndName(Pet pet, DiscordMessage nameMessage)
        {
            bool named = false;
            if (nameMessage.Content.Length > 255)
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

        private async Task<bool> ConfirmNaming(Pet pet, DiscordMessage nameMessage)
        {
            bool named;

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
                named = confirmResult.Result.Id == InteractionIds.Pets.NameConfirm;
            }
            else
            {
                await confirmationMessage.DeleteAsync();
                await nameMessage.RespondAsync(GetNamingTimedOutMessage(pet));
                named = true;
            }
            return named;
        }

        public bool HasSpaceForAnotherPet(DiscordMember user)
        {
            var capacity = GetPetCapacity(user.Guild.Id, user.Id);
            var ownedPets = GetNumberOfOwnedPets(user.Id);

            return ownedPets < capacity;
        }

        private static string GetAge(DateTime birthdate)
        {
            var age = DateTime.UtcNow - birthdate;
            var ageStr = age.Humanize(maxUnit: Humanizer.Localisation.TimeUnit.Year);
            return string.Concat(ageStr, " old");
        }

        private double GetSearchSuccessProbability(DiscordMember userSearching)
        {
            var ownedPets = (double)GetNumberOfOwnedPets(userSearching.Id);
            var probability = 2 / ownedPets;
            return Math.Min(1, probability);
        }

        private double GetBefriendSuccessProbability(DiscordMember user, Pet target)
        {
            const double baseRate = 0.1;

            var rarityModifier = RandomNumberGenerator.GetInt32((int)target.Rarity + 1);
            var petCapacity = (double)GetPetCapacity(user.Guild.Id, user.Id);
            var ownedPets = (double)GetNumberOfOwnedPets(user.Id);
            return baseRate + ((petCapacity - ownedPets) / (petCapacity + rarityModifier));
        }

        private int GetPetCapacity(ulong guildId, ulong userId)
        {
            int result = 1;

            if (Cache.Users.TryGetUser(guildId, userId, out var user))
            {
                result += (user.CurrentLevel / NewPetSlotUnlockLevels);
            }
            return result;
        }

        private int GetRequiredLevelForPet(int petNumber)
        {
            int additionalPetNumber = petNumber - 1;
            return additionalPetNumber * NewPetSlotUnlockLevels;
        }

        private int GetNumberOfOwnedPets(ulong userId)
        {
            int result = 0;
            if (Cache.Pets.TryGetUsersPets(userId, out var pets))
            {
                result = pets.Count;
            }
            return result;
        }
    }
}
