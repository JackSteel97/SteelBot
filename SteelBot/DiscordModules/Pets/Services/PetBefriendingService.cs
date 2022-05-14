﻿using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using SteelBot.Database.Models.Pets;
using SteelBot.Database.Models.Users;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Generation;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.Helpers;
using SteelBot.Helpers.Constants;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Services
{
    public class PetBefriendingService
    {
        private readonly DataCache Cache;
        private readonly PetFactory PetFactory;
        private readonly ErrorHandlingService ErrorHandlingService;
        private readonly ILogger<PetBefriendingService> Logger;

        public PetBefriendingService(DataCache cache, PetFactory petFactory, ErrorHandlingService errorHandlingService, ILogger<PetBefriendingService> logger)
        {
            Cache = cache;
            PetFactory = petFactory;
            ErrorHandlingService = errorHandlingService;
            Logger = logger;
        }

        public async Task Search(CommandContext context)
        {
            bool isReplacementBefriend = false;
            if (!HasSpaceForAnotherPet(context.Member))
            {
                isReplacementBefriend = CanReplaceToBefriend(context.Member);
                if (!isReplacementBefriend)
                {
                    Logger.LogInformation("User {UserId} cannot search for a new pet because their pet slots are already full", context.Member.Id);
                    await context.RespondAsync(EmbedGenerator.Warning($"You don't have enough room for another pet{Environment.NewLine}Use `Pet Manage` to release one of your existing pets to make room"), mention: true);
                    return;
                }
                Logger.LogInformation("User {UserId} is performing a replacement search", context.Member.Id);
            }

            if (!SearchSuccess(context.Member))
            {
                Logger.LogInformation("User {UserId} failed to find anything when searching for a new pet", context.Member.Id);
                await context.RespondAsync(EmbedGenerator.Info($"You didn't find anything this time!{Environment.NewLine}Try again later", "Nothing Found"), mention: true);
                return;
            }

            (bool befriendAttempt, Pet pet, DiscordInteraction interaction) = await HandleInitialSearchSuccess(context, isReplacementBefriend);
            bool newPet = false;
            if (befriendAttempt)
            {
                newPet = await HandleBefriendAttempt(context, pet, interaction);
            }

            if (newPet)
            {
                await context.RespondAsync(PetMessages.GetPetOwnedSuccessMessage(context.Member, pet), mention: true);
            }
        }

        private async Task<(bool befriendAttempt, Pet pet, DiscordInteraction interaction)> HandleInitialSearchSuccess(CommandContext context, bool mustReplace)
        {
            bool befriendAttempt = false;
            DiscordInteraction interaction = null;
            Cache.Users.TryGetUser(context.Guild.Id, context.Member.Id, out var user);
            var foundPet = PetFactory.Generate(user?.CurrentLevel ?? 0);
            var initialPetDisplay = PetDisplayHelpers.GetPetDisplayEmbed(foundPet, includeName: false);

            const string replacementWarning = "Warning: If you befriend this pet you must choose an existing one to release.";
            var initialResponseBuilder = new DiscordMessageBuilder()
                .WithContent($"You found a new potential friend!{(mustReplace ? Environment.NewLine + replacementWarning : "")}")
                .WithEmbed(initialPetDisplay)
                .AddComponents(new DiscordComponent[] {
                    Interactions.Pets.Befriend,
                    Interactions.Pets.Leave
                });

            Logger.LogInformation("Sending pet found message to User {UserId} in Guild {GuildId}", context.User.Id, context.Guild.Id);
            var message = await context.RespondAsync(initialResponseBuilder, mention: true);
            var result = await message.WaitForButtonAsync(context.Member);

            if (!result.TimedOut)
            {
                initialResponseBuilder.ClearComponents();
                message.ModifyAsync(initialResponseBuilder).FireAndForget(ErrorHandlingService);
                befriendAttempt = result.Result.Id == InteractionIds.Pets.Befriend;
                interaction = result.Result.Interaction;
            }
            else
            {
                Logger.LogInformation("Pet found message timed out waiting for a user response from User {UserId} in Guild {GuildId}", context.User.Id, context.Guild.Id);
                message.DeleteAsync().FireAndForget(ErrorHandlingService);
                context.RespondAsync(PetMessages.GetPetRanAwayMessage(foundPet)).FireAndForget(ErrorHandlingService);
            }

            return (befriendAttempt, foundPet, interaction);
        }

        private async Task<bool> HandleBefriendAttempt(CommandContext context, Pet pet, DiscordInteraction interaction)
        {
            Logger.LogInformation("User {UserId} in Guild {GuildId} is attempting to befriend a {Rarity} pet", context.User.Id, context.Guild.Id, pet.Rarity);
            bool befriendSuccess = false;
            bool hasSpace = HasSpaceForAnotherPet(context.Member);
            bool replacementBefriend = !hasSpace && CanReplaceToBefriend(context.Member);

            if ((hasSpace || replacementBefriend) && BefriendSuccess(context.Member, pet))
            {
                if (replacementBefriend)
                {
                    (befriendSuccess, interaction) = await HandleReplacingBefriend(context, pet);
                }
                else
                {
                    await HandleNonReplacingBefriend(context, pet);
                    befriendSuccess = true;
                }

                if (befriendSuccess)
                {
                    Logger.LogInformation("User {UserId} in Guild {GuildId} successfully befriended a {Rarity} pet with Id {PetId}", context.User.Id, context.Guild.Id, pet.Rarity, pet.RowId);

                    if (PetCorrupted() && Cache.Users.TryGetUser(context.Guild.Id, context.Member.Id, out var ownerUser))
                    {
                        Logger.LogInformation("Pet {PetId} became corrupted after being befriended", pet.RowId);
                        pet = PetBonusFactory.Corrupt(pet, ownerUser.CurrentLevel);
                        await Cache.Pets.UpdatePet(pet);
                        var response = PetMessages.GetPetCorruptedMessage(pet).WithReply(context.Message.Id, mention: true);
                        context.Channel.SendMessageAsync(response).FireAndForget(ErrorHandlingService);
                    }

                    await PetModals.NamePet(interaction, pet);
                }
            }
            else
            {
                var response = PetMessages.GetBefriendFailedMessage(pet).WithReply(context.Message.Id, mention: true);
                context.Channel.SendMessageAsync(response).FireAndForget(ErrorHandlingService);
            }
            return befriendSuccess;
        }

        private async Task<(bool befriendSuccess, DiscordInteraction interaction)> HandleReplacingBefriend(CommandContext context, Pet newPet)
        {
            DiscordInteraction interaction = null;
            if (Cache.Users.TryGetUser(context.Guild.Id, context.User.Id, out var user)
               && Cache.Pets.TryGetUsersPets(context.User.Id, out var allPets))
            {
                var availablePets = PetShared.GetAvailablePets(user, allPets, out var disabledPets);
                var combinedPets = PetShared.Recombine(availablePets, disabledPets);

                var baseEmbed = PetShared.GetOwnedPetsBaseEmbed(user, availablePets, disabledPets.Count > 0);

                var maxCapacity = PetShared.GetPetCapacity(user, allPets);
                var baseCapacity = PetShared.GetBasePetCapacity(user);

                var pages = PaginationHelper.GenerateEmbedPages(baseEmbed, combinedPets, 10,
                       (builder, pet) => PetShared.AppendPetDisplayShort(builder, pet.Pet, pet.Active, baseCapacity, maxCapacity),
                       (pet) => Interactions.Pets.Replace(pet.Pet.RowId, pet.Pet.GetName()));

                (string resultId, interaction) = await InteractivityHelper.SendPaginatedMessageWithComponentsAsync(context.Channel, context.User, pages);

                // Figure out which pet they want to replace.
                if (!string.IsNullOrWhiteSpace(resultId) &&
                    PetShared.TryGetPetIdFromComponentId(resultId, out var petId))
                {
                    await ReplacePetWith(context, petId, newPet);
                }
                else
                {
                    context.Channel.SendMessageAsync(PetMessages.GetPetRanAwayMessage(newPet)).FireAndForget(ErrorHandlingService);
                }
            }
            // Successfully replaced?
            return (newPet.RowId != default, interaction);
        }

        private async Task ReplacePetWith(CommandContext context, long petId, Pet newPet)
        {
            if (Cache.Pets.TryGetPet(context.Member.Id, petId, out var petToReplace))
            {
                var priority = petToReplace.Priority;
                await Cache.Pets.RemovePet(context.Member.Id, petId);
                newPet.Priority = priority;
                await AddPet(context.Member.Id, newPet);
            }
        }

        private async Task HandleNonReplacingBefriend(CommandContext context, Pet pet)
        {
            Cache.Pets.TryGetUsersPetsCount(context.Member.Id, out int numberOfOwnedPets);
            pet.Priority = numberOfOwnedPets;
            await AddPet(context.Member.Id, pet);
        }

        private async Task AddPet(ulong userId, Pet pet)
        {
            pet.OwnerDiscordId = userId;
            pet.RowId = await Cache.Pets.InsertPet(pet);
        }

        private bool SearchSuccess(DiscordMember userSearching)
        {
            var probability = GetSearchSuccessProbability(userSearching);
            Logger.LogDebug("Search success probability for User {UserId} is {Probability}", userSearching.Id, probability);
            return MathsHelper.TrueWithProbability(probability);
        }

        private double GetSearchSuccessProbability(DiscordMember userSearching)
        {
            int ownedPetCount = 0;
            double bonusMultiplier = 1;
            if (Cache.Users.TryGetUser(userSearching.Guild.Id, userSearching.Id, out var user)
                && Cache.Pets.TryGetUsersPets(userSearching.Id, out var ownedPets))
            {
                var activePets = PetShared.GetAvailablePets(user, ownedPets, out _);
                ownedPetCount = ownedPets.Count;

                bonusMultiplier = PetShared.GetBonusValue(activePets, BonusType.SearchSuccessRate);
            }

            var probability = (2D / ownedPetCount) * bonusMultiplier;
            return Math.Min(1, probability);
        }

        private bool HasSpaceForAnotherPet(DiscordMember user)
        {
            if (Cache.Users.TryGetUser(user.Guild.Id, user.Id, out var dbUser))
            {
                int capacity;
                if (Cache.Pets.TryGetUsersPets(user.Id, out var allPets))
                {
                    capacity = PetShared.GetPetCapacity(dbUser, allPets);
                }
                else
                {
                    capacity = PetShared.GetBasePetCapacity(dbUser);
                }

                return allPets.Count < capacity;
            }
            return false;
        }

        private bool CanReplaceToBefriend(DiscordMember user)
        {
            if (Cache.Users.TryGetUser(user.Guild.Id, user.Id, out var dbUser))
            {
                int capacity;
                if (Cache.Pets.TryGetUsersPets(user.Id, out var allPets))
                {
                    capacity = PetShared.GetPetCapacity(dbUser, allPets);
                }
                else
                {
                    capacity = PetShared.GetBasePetCapacity(dbUser);
                }

                return allPets.Count < (capacity + 1);
            }
            return false;
        }

        private bool BefriendSuccess(DiscordMember user, Pet target)
        {
            if (Cache.Users.TryGetUser(user.Guild.Id, user.Id, out var dbUser))
            {
                var probability = GetBefriendSuccessProbability(dbUser, target);
                Logger.LogDebug("Befriend success probability for User {UserId} and Pet Rarity {Rarity} is {Probability}", user.Id, target.Rarity, probability);
                return MathsHelper.TrueWithProbability(probability);
            }
            return false;
        }

        private double GetBefriendSuccessProbability(User user, Pet target)
        {
            const double baseRate = 0.1;
            int ownedPetCount = 0;
            double bonusMultiplier = 1;
            double petCapacity;
            if (Cache.Pets.TryGetUsersPets(user.DiscordId, out var allPets))
            {
                var activePets = PetShared.GetAvailablePets(user, allPets, out _);
                bonusMultiplier = PetShared.GetBonusValue(activePets, BonusType.BefriendSuccessRate);
                ownedPetCount = allPets.Count;
                petCapacity = PetShared.GetPetCapacity(user, allPets);
            }
            else
            {
                petCapacity = PetShared.GetBasePetCapacity(user);
            }

            var rarityModifier = RandomNumberGenerator.GetInt32((int)target.Rarity + 1);
            var currentRate = baseRate + ((petCapacity - ownedPetCount) / (petCapacity + rarityModifier));
            return currentRate * bonusMultiplier;
        }

        private static bool PetCorrupted() => MathsHelper.TrueWithProbability(0.001);
    }
}
