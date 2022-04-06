using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using SteelBot.Database.Models;
using SteelBot.Database.Models.Pets;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Generation;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.Helpers;
using SteelBot.Helpers.Constants;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
using System;
using System.Collections.Generic;
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
            if (!HasSpaceForAnotherPet(context.Member))
            {
                Logger.LogInformation("User {UserId} cannot search for a new pet because their pet slots are already full", context.Member.Id);
                await context.RespondAsync(EmbedGenerator.Warning($"You don't have enough room for another pet{Environment.NewLine}Use `Pet Manage` to release one of your existing pets to make room"), mention: true);
                return;
            }

            if (!SearchSuccess(context.Member))
            {
                Logger.LogInformation("User {UserId} failed to find anything when searching for a new pet", context.Member.Id);
                await context.RespondAsync(EmbedGenerator.Info($"You didn't find anything this time!{Environment.NewLine}Try again later", "Nothing Found"), mention: true);
                return;
            }

            (bool befriendAttempt, Pet pet, DiscordInteraction interaction) = await HandleInitialSearchSuccess(context);
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

        private async Task<(bool befriendAttempt, Pet pet, DiscordInteraction interaction)> HandleInitialSearchSuccess(CommandContext context)
        {
            bool befriendAttempt = false;
            DiscordInteraction interaction = null;

            var foundPet = PetFactory.Generate();
            var initialPetDisplay = PetDisplayHelpers.GetPetDisplayEmbed(foundPet, includeName: false);

            var initialResponseBuilder = new DiscordMessageBuilder()
                .WithContent("You found a new potential friend!")
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
            if (HasSpaceForAnotherPet(context.Member) && BefriendSuccess(context.Member, pet))
            {
                Cache.Pets.TryGetUsersPetsCount(context.Member.Id, out int numberOfOwnedPets);
                pet.OwnerDiscordId = context.Member.Id;
                pet.Priority = numberOfOwnedPets;
                pet.RowId = await Cache.Pets.InsertPet(pet);

                Logger.LogInformation("User {UserId} in Guild {GuildId} successfully befriended a {Rarity} pet with Id {PetId}", context.User.Id, context.Guild.Id, pet.Rarity, pet.RowId);

                await PetModals.NamePet(interaction, pet);
                befriendSuccess = true;
            }
            else
            {
                var response = PetMessages.GetBefriendFailedMessage(pet).WithReply(context.Message.Id, mention: true);
                context.Channel.SendMessageAsync(response).FireAndForget(ErrorHandlingService);
            }
            return befriendSuccess;
        }

        private bool SearchSuccess(DiscordMember userSearching)
        {
            var probability = GetSearchSuccessProbability(userSearching);
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
                double bonusCapacity = 0;
                if (Cache.Pets.TryGetUsersPets(user.Id, out var allPets))
                {
                    var activePets = PetShared.GetAvailablePets(dbUser, allPets, out _);

                    bonusCapacity = PetShared.GetBonusValue(activePets, BonusType.PetSlots);
                }

                var capacity = PetShared.GetPetCapacity(dbUser, bonusCapacity);

                return allPets.Count < capacity;
            }
            return false;
        }

        private bool BefriendSuccess(DiscordMember user, Pet target)
        {
            if (Cache.Users.TryGetUser(user.Guild.Id, user.Id, out var dbUser))
            {
                var probability = GetBefriendSuccessProbability(dbUser, target);
                return MathsHelper.TrueWithProbability(probability);
            }
            return false;
        }

        private double GetBefriendSuccessProbability(User user, Pet target)
        {
            const double baseRate = 0.1;
            int ownedPetCount = 0;
            double bonusCapacity = 0;
            double bonusMultiplier = 1;
            if (Cache.Pets.TryGetUsersPets(user.DiscordId, out var allPets))
            {
                var activePets = PetShared.GetAvailablePets(user, allPets, out _);
                bonusCapacity = PetShared.GetBonusValue(activePets, BonusType.PetSlots);
                bonusMultiplier = PetShared.GetBonusValue(activePets, BonusType.BefriendSuccessRate);
                ownedPetCount = allPets.Count;
            }

            var rarityModifier = RandomNumberGenerator.GetInt32((int)target.Rarity + 1);
            var petCapacity = (double)PetShared.GetPetCapacity(user, bonusCapacity);
            var currentRate = baseRate + ((petCapacity - ownedPetCount) / (petCapacity + rarityModifier));
            return currentRate * bonusMultiplier;
        }
    }
}
