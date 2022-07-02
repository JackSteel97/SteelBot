using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Sentry;
using SteelBot.Database.Models.Pets;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.Helpers;
using SteelBot.Helpers.Constants;
using SteelBot.Helpers.Extensions;
using SteelBot.Helpers.Sentry;
using SteelBot.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Services;

public class PetManagementService
{
    private readonly DataCache _cache;
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly IHub _sentry;

    public PetManagementService(DataCache cache, ErrorHandlingService errorHandlingService, IHub sentry)
    {
        _cache = cache;
        _errorHandlingService = errorHandlingService;
        _sentry = sentry;
    }

    public async Task Manage(CommandContext context)
    {
        var transaction = _sentry.GetCurrentTransaction();
        if (_cache.Users.TryGetUser(context.Guild.Id, context.User.Id, out var user)
            && _cache.Pets.TryGetUsersPets(context.User.Id, out var allPets))
        {
            var getPetsSpan = transaction.StartChild("Get Available Pets");
            var availablePets = PetShared.GetAvailablePets(user, allPets, out var disabledPets);
            var combinedPets = PetShared.Recombine(availablePets, disabledPets);
            getPetsSpan.Finish();

            var buildEmbedSpan = transaction.StartChild("Build Owned Pets Base Embed");
            var baseEmbed = PetShared.GetOwnedPetsBaseEmbed(user, availablePets, disabledPets.Count > 0);
            buildEmbedSpan.Finish();

            var capacitySpan = transaction.StartChild("Get Pet Capacity");
            int maxCapacity = PetShared.GetPetCapacity(user, allPets);
            int baseCapacity = PetShared.GetBasePetCapacity(user);
            capacitySpan.Finish();

            var pagesSpan = transaction.StartChild("Generate Embed Pages");
            var pages = PaginationHelper.GenerateEmbedPages(baseEmbed, combinedPets, 10,
                (builder, pet) => PetShared.AppendPetDisplayShort(builder, pet.Pet, pet.Active, baseCapacity, maxCapacity),
                (pet) => Interactions.Pets.Manage(pet.Pet.RowId, pet.Pet.GetName()));
            pagesSpan.Finish();

            transaction.Finish(SpanStatus.Ok);
            (string resultId, _) = await InteractivityHelper.SendPaginatedMessageWithComponentsAsync(context.Channel, context.User, pages);
            var responseTransaction = _sentry.StartNewConfiguredTransaction(nameof(PetManagementService), "Handle Manage Response");
            if (!string.IsNullOrWhiteSpace(resultId))
            {
                var handleSpan = responseTransaction.StartChild("Handle Manage Pet");
                // Figure out which pet they want to manage.
                if (PetShared.TryGetPetIdFromComponentId(resultId, out long petId))
                {
                    await HandleManagePet(context, petId, handleSpan);
                }
                handleSpan.Finish();
            }
        }
        else
        {
            context.RespondAsync(PetMessages.GetNoPetsAvailableMessage(), mention: true).FireAndForget(_errorHandlingService);
        }
    }

    public async Task MovePetToPosition(Pet petBeingMoved, int newPriority)
    {
        if (_cache.Pets.TryGetUsersPets(petBeingMoved.OwnerDiscordId, out var allPets))
        {
            if (newPriority < 0 || newPriority > allPets.Count - 1)
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
                }
                else
                {
                    petBeingMoved.Priority = newPriority;
                }
            }

            await _cache.Pets.UpdatePets(allPets);
        }
    }

    private async Task HandleManagePet(CommandContext context, long petId, ISpan transaction)
    {
        if (_cache.Pets.TryGetPet(context.Member.Id, petId, out var pet))
        {
            var countPetsSpan = transaction.StartChild("Count Pets");
            _cache.Pets.TryGetUsersPetsCount(context.Member.Id, out int ownedPetCount);
            countPetsSpan.Finish();

            var buildMessageSpan = transaction.StartChild("Build Message");
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
            buildMessageSpan.Finish();

            var waitingForResponseSpan = transaction.StartChild("Wait for user selection");
            var message = await context.RespondAsync(initialResponseBuilder, mention: true);
            var result = await message.WaitForButtonAsync(context.Member);
            waitingForResponseSpan.Finish();
            initialResponseBuilder.ClearComponents();

            if (!result.TimedOut && result.Result.Id != InteractionIds.Confirmation.Cancel)
            {
                message.ModifyAsync(initialResponseBuilder).FireAndForget(_errorHandlingService);
                var performManageSpan = transaction.StartChild("Perform Manage Operation", result.Result.Id);
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

                performManageSpan.Finish();
            }
            else
            {
                message.ModifyAsync(initialResponseBuilder).FireAndForget(_errorHandlingService);
            }
        }
        else
        {
            context.RespondAsync(EmbedGenerator.Error("Something went wrong and I couldn't find that pet. Please try again later."), mention: true).FireAndForget(_errorHandlingService);
        }
    }

    private async Task HandleMakePrimary(CommandContext context, Pet pet)
    {
        int oldPriority = pet.Priority;
        if (_cache.Users.TryGetUser(context.Guild.Id, context.User.Id, out _)
            && _cache.Pets.TryGetUsersPets(context.Member.Id, out var allPets))
        {
            var petsToUpdate = new List<Pet>(oldPriority + 1);

            foreach (var ownedPet in allPets)
            {
                if (ownedPet.Priority < oldPriority)
                {
                    ++ownedPet.Priority;
                    petsToUpdate.Add(ownedPet);
                }
            }

            pet.Priority = 0;
            petsToUpdate.Add(pet);
            await _cache.Pets.UpdatePets(petsToUpdate);

            context.Channel.SendMessageAsync(PetMessages.GetMakePrimarySuccessMessage(pet)).FireAndForget(_errorHandlingService);
        }
    }

    private async Task HandleMoveToBottom(CommandContext context, Pet pet)
    {
        int oldPriority = pet.Priority;
        if (_cache.Users.TryGetUser(context.Guild.Id, context.User.Id, out _)
            && _cache.Pets.TryGetUsersPets(context.Member.Id, out var allPets))
        {
            var petsToUpdate = new List<Pet>(allPets.Count - oldPriority);

            foreach (var ownedPet in allPets)
            {
                if (ownedPet.Priority > oldPriority)
                {
                    --ownedPet.Priority;
                    petsToUpdate.Add(ownedPet);
                }
            }
            pet.Priority = allPets.Count - 1;
            petsToUpdate.Add(pet);

            await _cache.Pets.UpdatePets(petsToUpdate);
            context.Channel.SendMessageAsync(PetMessages.GetMoveToBottomSuccessMessage(pet)).FireAndForget(_errorHandlingService);
        }
    }

    private async Task HandlePriorityIncrease(CommandContext context, Pet pet)
    {
        int oldPriority = pet.Priority;
        if (_cache.Users.TryGetUser(context.Guild.Id, context.User.Id, out _)
            && _cache.Pets.TryGetUsersPets(context.Member.Id, out var allPets))
        {
            var petsToUpdate = new List<Pet>(2);
            foreach (var ownedPet in allPets)
            {
                if (ownedPet.Priority == oldPriority - 1)
                {
                    ++ownedPet.Priority;
                    petsToUpdate.Add(ownedPet);
                    break;
                }
            }
            --pet.Priority;
            petsToUpdate.Add(pet);
            await _cache.Pets.UpdatePets(petsToUpdate);

            context.Channel.SendMessageAsync(PetMessages.GetPriorityIncreaseSuccessMessage(pet)).FireAndForget(_errorHandlingService);
        }
    }

    private async Task HandlePriorityDecrease(CommandContext context, Pet pet)
    {
        int oldPriority = pet.Priority;
        if (_cache.Users.TryGetUser(context.Guild.Id, context.User.Id, out _)
            && _cache.Pets.TryGetUsersPets(context.Member.Id, out var allPets))
        {
            var petsToUpdate = new List<Pet>(2);
            foreach (var ownedPet in allPets)
            {
                if (ownedPet.Priority == oldPriority + 1)
                {
                    --ownedPet.Priority;
                    petsToUpdate.Add(ownedPet);
                    break;
                }
            }
            ++pet.Priority;
            petsToUpdate.Add(pet);
            await _cache.Pets.UpdatePets(petsToUpdate);

            context.Channel.SendMessageAsync(PetMessages.GetPriorityDecreaseSuccessMessage(pet)).FireAndForget(_errorHandlingService);
        }
    }

    private async Task HandlePetAbandon(CommandContext context, Pet pet)
    {
        var transaction = _sentry.GetCurrentTransaction();
        transaction.Finish();
        if (await InteractivityHelper.GetConfirmation(context, "Pet Release"))
        {
            var abandonTransaction = _sentry.StartNewConfiguredTransaction(nameof(PetManagementService), nameof(HandlePetAbandon));

            await _cache.Pets.RemovePet(context.Member.Id, pet.RowId);

            if (_cache.Pets.TryGetUsersPets(context.Member.Id, out var allPets))
            {
                var petsToUpdate = new List<Pet>(allPets.Count - pet.Priority);
                foreach (var ownedPet in allPets)
                {
                    if (ownedPet.Priority > pet.Priority)
                    {
                        --ownedPet.Priority;
                        petsToUpdate.Add(ownedPet);
                    }
                }
                await _cache.Pets.UpdatePets(petsToUpdate);
            }

            context.Channel.SendMessageAsync(PetMessages.GetAbandonSuccessMessage(pet)).FireAndForget(_errorHandlingService);
        }
    }
}