using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using SteelBot.Database.Models;
using SteelBot.Database.Models.Pets;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets.Generation;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.Helpers;
using SteelBot.Helpers.Constants;
using SteelBot.Helpers.Extensions;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Services
{
    public class PetBefriendingService
    {
        private readonly DataCache Cache;
        private readonly PetFactory PetFactory;

        public PetBefriendingService(DataCache cache, PetFactory petFactory)
        {
            Cache = cache;
            PetFactory = petFactory;
        }

        public async Task Search(CommandContext context)
        {
            if (!SearchSuccess(context.Member))
            {
                await context.RespondAsync(EmbedGenerator.Info($"You didn't find anything this time!{Environment.NewLine}Try again later", "Nothing Found"), mention: true);
                return;
            }

            (bool befriendAttempt, Pet pet) = await HandleInitialSearchSuccess(context);
            bool newPet = false;
            if (befriendAttempt)
            {
                newPet = await HandleBefriendAttempt(context, pet);
            }

            if (newPet)
            {
                await context.RespondAsync(PetMessages.GetPetOwnedSuccessMessage(context.Member, pet), mention: true);
            }
        }

        private async Task<(bool befriendAttempt, Pet pet)> HandleInitialSearchSuccess(CommandContext context)
        {
            bool befriendAttempt = false;
            var hasSpace = HasSpaceForAnotherPet(context.Member);
            string noSpaceMessage = "";
            if (!hasSpace)
            {
                noSpaceMessage = " But you don't have enough room for another pet!";
            }

            var foundPet = PetFactory.Generate();
            var initialPetDisplay = PetDisplayHelpers.GetPetDisplayEmbed(foundPet, includeName: false);

            var initialResponseBuilder = new DiscordMessageBuilder()
                .WithContent($"You found a new potential friend!{noSpaceMessage}")
                .WithEmbed(initialPetDisplay)
                .AddComponents(new DiscordComponent[] {
                    Interactions.Pets.Befriend.Disable(!hasSpace),
                    Interactions.Pets.Leave
                });

            var message = await context.RespondAsync(initialResponseBuilder, mention: true);
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
                await context.RespondAsync(PetMessages.GetPetRanAwayMessage(foundPet), mention: true);
            }

            return (befriendAttempt, foundPet);
        }

        private async Task<bool> HandleBefriendAttempt(CommandContext context, Pet pet)
        {
            bool befriendSuccess = BefriendSuccess(context.Member, pet);
            if (befriendSuccess)
            {
                Cache.Pets.TryGetUsersPetsCount(context.Member.Id, out int numberOfOwnedPets);
                pet.OwnerDiscordId = context.Member.Id;
                pet.Priority = numberOfOwnedPets;
                await HandleBefriendSuccess(context, pet);
                await Cache.Pets.InsertPet(pet);
            }
            else
            {
                var response = PetMessages.GetBefriendFailedMessage(pet).WithReply(context.Message.Id, mention: true);
                await context.Channel.SendMessageAsync(response);
            }
            return befriendSuccess;
        }

        public static async Task HandleBefriendSuccess(CommandContext context, Pet pet)
        {
            var successMessage = PetMessages.GetBefriendSuccessMessage(pet).WithReply(context.Message.Id, mention: true);
            await context.Channel.SendMessageAsync(successMessage);
            await PetShared.HandleNamingPet(context, pet);
        }

        private bool SearchSuccess(DiscordMember userSearching)
        {
            var probability = GetSearchSuccessProbability(userSearching);
            return MathsHelper.TrueWithProbability(probability);
        }

        private double GetSearchSuccessProbability(DiscordMember userSearching)
        {
            Cache.Pets.TryGetUsersPetsCount(userSearching.Id, out var ownedPets);

            var probability = 2D / ownedPets;
            return Math.Min(1, probability);
        }

        private bool HasSpaceForAnotherPet(DiscordMember user)
        {
            if (Cache.Users.TryGetUser(user.Guild.Id, user.Id, out var dbUser))
            {
                var capacity = PetShared.GetPetCapacity(dbUser);
                Cache.Pets.TryGetUsersPetsCount(user.Id, out int ownedPets);
                return ownedPets < capacity;
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

            var rarityModifier = RandomNumberGenerator.GetInt32((int)target.Rarity + 1);
            var petCapacity = (double)PetShared.GetPetCapacity(user);
            Cache.Pets.TryGetUsersPetsCount(user.DiscordId, out var ownedPets);
            return baseRate + ((petCapacity - ownedPets) / (petCapacity + rarityModifier));
        }
    }
}
