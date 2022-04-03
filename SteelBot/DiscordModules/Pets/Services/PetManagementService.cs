using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using SteelBot.Database.Models.Pets;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.Helpers;
using SteelBot.Helpers.Constants;
using SteelBot.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Services
{
    public class PetManagementService
    {
        private readonly DataCache Cache;

        public PetManagementService(DataCache cache)
        {
            Cache = cache;
        }

        public async Task Manage(CommandContext context)
        {
            if (Cache.Users.TryGetUser(context.Guild.Id, context.User.Id, out var user)
                && Cache.Pets.TryGetUsersPets(context.User.Id, out var allPets))
            {
                var availablePets = PetShared.GetAvailablePets(user, allPets, out var disabledPets);
                var combinedPets = PetShared.Recombine(availablePets, disabledPets);

                var baseEmbed = PetShared.GetOwnedPetsBaseEmbed(user, availablePets, disabledPets);

                if (combinedPets.Count == 0)
                {
                    baseEmbed.WithDescription("You currently own no pets.");
                    await context.RespondAsync(baseEmbed);
                    return;
                }

                var bonusCapacity = PetShared.GetBonusValue(availablePets, Enums.BonusType.PetSlots);
                var maxCapacity = PetShared.GetPetCapacity(user, bonusCapacity);
                var baseCapacity = PetShared.GetPetCapacity(user, 0);

                var pages = PaginationHelper.GenerateEmbedPages(baseEmbed, combinedPets, 10,
                    (builder, pet) => PetShared.AppendPetDisplayShort(builder, pet.Pet, pet.Active, baseCapacity, maxCapacity),
                    (pet) => Interactions.Pets.Manage(pet.Pet.RowId, pet.Pet.GetName()));

                var resultId = await InteractivityHelper.SendPaginatedMessageWithComponentsAsync(context.Channel, context.User, pages);
                if (!string.IsNullOrWhiteSpace(resultId))
                {
                    // Figure out which pet they want to manage.
                    if (PetShared.TryGetPetIdFromComponentId(resultId, out var petId))
                    {
                        await HandleManagePet(context, petId);
                    }
                }
            }
            else
            {
                await context.RespondAsync(PetMessages.GetNoPetsAvailableMessage(), mention: true);
            }
        }

        public async Task MovePetToPosition(Pet petBeingMoved, int newPriority)
        {
            if (Cache.Pets.TryGetUsersPets(petBeingMoved.OwnerDiscordId, out var allPets))
            {
                if(newPriority < 0 || newPriority > allPets.Count-1)
                {
                    // Invalid target position.
                    return;
                }

                int oldPriority = petBeingMoved.Priority;

                foreach (var ownedPet in allPets)
                {
                    if (ownedPet.RowId != petBeingMoved.RowId)
                    {
                        // "Remove" behaviour
                        if (ownedPet.Priority > oldPriority)
                        {
                            ownedPet.Priority--;
                        }

                        // "Insert" behaviour
                        if (ownedPet.Priority >= newPriority)
                        {
                            ownedPet.Priority++;
                        }

                        await Cache.Pets.UpdatePet(ownedPet);
                    }
                }

                petBeingMoved.Priority = newPriority;
                await Cache.Pets.UpdatePet(petBeingMoved);
            }
        }

        private async Task HandleManagePet(CommandContext context, long petId)
        {
            if (Cache.Pets.TryGetPet(context.Member.Id, petId, out var pet))
            {
                Cache.Pets.TryGetUsersPetsCount(context.Member.Id, out var ownedPetCount);
                var petDisplay = PetDisplayHelpers.GetPetDisplayEmbed(pet, showLevelProgress: true);
                var initialResponseBuilder = new DiscordMessageBuilder()
                    .WithEmbed(petDisplay);

                initialResponseBuilder = InteractivityHelper.AddComponents(initialResponseBuilder, new DiscordComponent[]
                    {
                        Interactions.Pets.MakePrimary.Disable(pet.IsPrimary),
                        Interactions.Pets.IncreasePriority.Disable(pet.IsPrimary),
                        Interactions.Pets.MoveToPosition.Disable(ownedPetCount <= 1),
                        Interactions.Pets.DecreasePriority.Disable(pet.Priority == (ownedPetCount-1)),
                        Interactions.Pets.MoveToBottom.Disable(pet.Priority == (ownedPetCount-1)),
                        Interactions.Pets.Rename,
                        Interactions.Pets.Abandon,
                        Interactions.Confirmation.Cancel,
                    });

                var message = await context.RespondAsync(initialResponseBuilder, mention: true);
                var result = await message.WaitForButtonAsync(context.Member);

                initialResponseBuilder.ClearComponents();

                if (!result.TimedOut && result.Result.Id != InteractionIds.Confirmation.Cancel)
                {
                    await message.ModifyAsync(initialResponseBuilder);
                    switch (result.Result.Id)
                    {
                        case InteractionIds.Pets.Rename:
                            await PetModals.NamePet(result.Result.Interaction, pet);
                            break;
                        case InteractionIds.Pets.MakePrimary:
                            await HandleMakePrimary(context, pet);
                            break;
                        case InteractionIds.Pets.IncreasePriority:
                            await HandlePriorityIncrease(context, pet);
                            break;
                        case InteractionIds.Pets.DecreasePriority:
                            await HandlePriorityDecrease(context, pet);
                            break;
                        case InteractionIds.Pets.MoveToBottom:
                            await HandleMoveToBottom(context, pet);
                            break;
                        case InteractionIds.Pets.Abandon:
                            await HandlePetAbandon(context, pet);
                            break;
                        case InteractionIds.Pets.MoveToButton:
                            await PetModals.MovePet(result.Result.Interaction, pet, ownedPetCount);
                            break;
                    }
                }
                else
                {
                    await message.ModifyAsync(initialResponseBuilder);
                }
            }
            else
            {
                await context.RespondAsync(EmbedGenerator.Error("Something went wrong and I couldn't find that pet. Please try again later."), mention: true);
            }
        }

        private async Task HandleMakePrimary(CommandContext context, Pet pet)
        {
            int oldPriority = pet.Priority;
            if (Cache.Users.TryGetUser(context.Guild.Id, context.User.Id, out var user)
                && Cache.Pets.TryGetUsersPets(context.Member.Id, out var allPets))
            {
                int originalCapacity = PetShared.GetPetCapacityFromAllPets(user, allPets);
                foreach (var ownedPet in allPets)
                {
                    if (ownedPet.Priority < oldPriority)
                    {
                        ++ownedPet.Priority;
                        await Cache.Pets.UpdatePet(ownedPet);
                    }
                }
                pet.Priority = 0;
                await Cache.Pets.UpdatePet(pet);
                await context.Channel.SendMessageAsync(PetMessages.GetMakePrimarySuccessMessage(pet));

                int newCapacity = PetShared.GetPetCapacityFromAllPets(user, allPets);
                if (newCapacity < originalCapacity)
                {
                    _ = context.Channel.SendMessageAsync(PetMessages.GetPetCapacityDecreasedMessage(originalCapacity, newCapacity));
                }
            }
        }

        private async Task HandleMoveToBottom(CommandContext context, Pet pet)
        {
            int oldPriority = pet.Priority;
            if (Cache.Users.TryGetUser(context.Guild.Id, context.User.Id, out var user)
                && Cache.Pets.TryGetUsersPets(context.Member.Id, out var allPets))
            {
                int originalCapacity = PetShared.GetPetCapacityFromAllPets(user, allPets);

                foreach (var ownedPet in allPets)
                {
                    if (ownedPet.Priority > oldPriority)
                    {
                        --ownedPet.Priority;
                        await Cache.Pets.UpdatePet(ownedPet);
                    }
                }
                pet.Priority = allPets.Count - 1;
                await Cache.Pets.UpdatePet(pet);
                await context.Channel.SendMessageAsync(PetMessages.GetMoveToBottomSuccessMessage(pet));

                int newCapacity = PetShared.GetPetCapacityFromAllPets(user, allPets);
                if (newCapacity < originalCapacity)
                {
                    _ = context.Channel.SendMessageAsync(PetMessages.GetPetCapacityDecreasedMessage(originalCapacity, newCapacity));
                }
            }
        }

        private async Task HandlePriorityIncrease(CommandContext context, Pet pet)
        {
            int oldPriority = pet.Priority;
            if (Cache.Users.TryGetUser(context.Guild.Id, context.User.Id, out var user)
                && Cache.Pets.TryGetUsersPets(context.Member.Id, out var allPets))
            {
                int originalCapacity = PetShared.GetPetCapacityFromAllPets(user, allPets);

                foreach (var ownedPet in allPets)
                {
                    if (ownedPet.Priority == oldPriority - 1)
                    {
                        ++ownedPet.Priority;
                        await Cache.Pets.UpdatePet(ownedPet);
                        break;
                    }
                }
                --pet.Priority;
                await Cache.Pets.UpdatePet(pet);
                await context.Channel.SendMessageAsync(PetMessages.GetPriorityIncreaseSuccessMessage(pet));

                int newCapacity = PetShared.GetPetCapacityFromAllPets(user, allPets);
                if (newCapacity < originalCapacity)
                {
                    _ = context.Channel.SendMessageAsync(PetMessages.GetPetCapacityDecreasedMessage(originalCapacity, newCapacity));
                }
            }
        }

        private async Task HandlePriorityDecrease(CommandContext context, Pet pet)
        {
            int oldPriority = pet.Priority;
            if (Cache.Users.TryGetUser(context.Guild.Id, context.User.Id, out var user)
                && Cache.Pets.TryGetUsersPets(context.Member.Id, out var allPets))
            {
                int originalCapacity = PetShared.GetPetCapacityFromAllPets(user, allPets);

                foreach (var ownedPet in allPets)
                {
                    if (ownedPet.Priority == oldPriority + 1)
                    {
                        --ownedPet.Priority;
                        await Cache.Pets.UpdatePet(ownedPet);
                        break;
                    }
                }
                ++pet.Priority;
                await Cache.Pets.UpdatePet(pet);
                await context.Channel.SendMessageAsync(PetMessages.GetPriorityDecreaseSuccessMessage(pet));

                int newCapacity = PetShared.GetPetCapacityFromAllPets(user, allPets);
                if (newCapacity < originalCapacity)
                {
                    _ = context.Channel.SendMessageAsync(PetMessages.GetPetCapacityDecreasedMessage(originalCapacity, newCapacity));
                }
            }
        }

        private async Task HandlePetAbandon(CommandContext context, Pet pet)
        {
            if (await InteractivityHelper.GetConfirmation(context, "Pet Release"))
            {
                await Cache.Pets.RemovePet(context.Member.Id, pet.RowId);

                if (Cache.Pets.TryGetUsersPets(context.Member.Id, out var allPets))
                {
                    foreach (var ownedPet in allPets)
                    {
                        if (ownedPet.Priority > pet.Priority)
                        {
                            --ownedPet.Priority;
                            await Cache.Pets.UpdatePet(ownedPet);
                        }
                    }
                }

                await context.Channel.SendMessageAsync(PetMessages.GetAbandonSuccessMessage(pet));
            }
        }
    }
}