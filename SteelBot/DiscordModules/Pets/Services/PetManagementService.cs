using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Sentry;
using SteelBot.Channels;
using SteelBot.Channels.Pets;
using SteelBot.Database.Models.Pets;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.Helpers;
using SteelBot.Helpers.Constants;
using SteelBot.Helpers.Extensions;
using SteelBot.Helpers.Sentry;
using SteelBot.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Services;

public class PetManagementService
{
    private readonly DataCache _cache;
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly IHub _sentry;
    private readonly CancellationService _cancellationService;

    public PetManagementService(DataCache cache, ErrorHandlingService errorHandlingService, IHub sentry, CancellationService cancellationService)
    {
        _cache = cache;
        _errorHandlingService = errorHandlingService;
        _sentry = sentry;
        _cancellationService = cancellationService;
    }

    public async Task Manage(PetCommandAction request)
    {
        var transaction = _sentry.GetCurrentTransaction();
        if (_cache.Users.TryGetUser(request.Guild.Id, request.Member.Id, out var user)
            && _cache.Pets.TryGetUsersPets(request.Member.Id, out var allPets))
        {
            var getPetsSpan = transaction.StartChild("Get Available Pets");
            var availablePets = PetShared.GetAvailablePets(user, allPets, out var disabledPets);
            var combinedPets = PetShared.Recombine(availablePets, disabledPets);
            getPetsSpan.Finish();

            var buildEmbedSpan = transaction.StartChild("Build Owned Pets Base Embed");
            var baseEmbed = PetShared.GetOwnedPetsBaseEmbed(user, combinedPets.ConvertAll(x=>x.Pet), disabledPets.Count > 0);
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

            var waitForUserResponseSpan = transaction.StartChild("Wait for User Choice");
            (string resultId, _) = await request.Responder.RespondPaginatedWithComponents(pages);
            waitForUserResponseSpan.Finish();
            if (!string.IsNullOrWhiteSpace(resultId))
            {
                // Figure out which pet they want to manage.
                if (PetShared.TryGetPetIdFromComponentId(resultId, out long petId))
                {
                    await ChannelsController.SendMessage(request with { Action = PetCommandActionType.ManageOne, PetId = petId }, _cancellationService.Token);
                }
            }
        }
        else
        {
            request.Responder.Respond(PetMessages.GetNoPetsAvailableMessage());
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

    public async Task ManagePet(PetCommandAction request, long petId)
    {
        if (_cache.Pets.TryGetPet(request.Member.Id, petId, out var pet))
        {
            var countPetsSpan = _sentry.StartSpanOnCurrentTransaction("Count Pets");
            _cache.Pets.TryGetUsersPetsCount(request.Member.Id, out int ownedPetCount);
            countPetsSpan.Finish();

            var buildMessageSpan = _sentry.StartSpanOnCurrentTransaction("Build Message");
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

            var waitingForResponseSpan = _sentry.StartSpanOnCurrentTransaction("Wait for user selection");
            var message = await request.Responder.RespondAsync(initialResponseBuilder);
            var result = await message.WaitForButtonAsync(request.Member);
            waitingForResponseSpan.Finish();
            initialResponseBuilder.ClearComponents();

            if (!result.TimedOut && result.Result.Id != InteractionIds.Confirmation.Cancel)
            {
                message.ModifyAsync(initialResponseBuilder).FireAndForget(_errorHandlingService);
                var performManageSpan = _sentry.StartSpanOnCurrentTransaction("Perform Manage Operation", result.Result.Id);
                switch (result.Result.Id)
                {
                    case InteractionIds.Pets.Rename:
                        await PetModals.NamePet(result.Result.Interaction, pet);
                        break;
                    case InteractionIds.Pets.MakePrimary:
                        await HandleMakePrimary(request, pet);
                        break;
                    case InteractionIds.Pets.IncreasePriority:
                        await HandlePriorityIncrease(request, pet);
                        break;
                    case InteractionIds.Pets.DecreasePriority:
                        await HandlePriorityDecrease(request, pet);
                        break;
                    case InteractionIds.Pets.MoveToBottom:
                        await HandleMoveToBottom(request, pet);
                        break;
                    case InteractionIds.Pets.Abandon:
                        await HandlePetAbandon(request, pet);
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
            request.Responder.Respond(new DiscordMessageBuilder().WithEmbed(EmbedGenerator.Error("Something went wrong and I couldn't find that pet. Please try again later.")));
        }
    }

    private async Task HandleMakePrimary(PetCommandAction request, Pet pet)
    {
        int oldPriority = pet.Priority;
        if (_cache.Users.TryGetUser(request.Guild.Id, request.Member.Id, out _)
            && _cache.Pets.TryGetUsersPets(request.Member.Id, out var allPets))
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

            request.Responder.Respond(PetMessages.GetMakePrimarySuccessMessage(pet));
        }
    }

    private async Task HandleMoveToBottom(PetCommandAction request, Pet pet)
    {
        int oldPriority = pet.Priority;
        if (_cache.Users.TryGetUser(request.Guild.Id, request.Member.Id, out _)
            && _cache.Pets.TryGetUsersPets(request.Member.Id, out var allPets))
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
            request.Responder.Respond(PetMessages.GetMoveToBottomSuccessMessage(pet));
        }
    }

    private async Task HandlePriorityIncrease(PetCommandAction request, Pet pet)
    {
        int oldPriority = pet.Priority;
        if (_cache.Users.TryGetUser(request.Guild.Id, request.Member.Id, out _)
            && _cache.Pets.TryGetUsersPets(request.Member.Id, out var allPets))
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

            request.Responder.Respond(PetMessages.GetPriorityIncreaseSuccessMessage(pet));
        }
    }

    private async Task HandlePriorityDecrease(PetCommandAction request, Pet pet)
    {
        int oldPriority = pet.Priority;
        if (_cache.Users.TryGetUser(request.Guild.Id, request.Member.Id, out _)
            && _cache.Pets.TryGetUsersPets(request.Member.Id, out var allPets))
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

            request.Responder.Respond(PetMessages.GetPriorityDecreaseSuccessMessage(pet));
        }
    }

    private async Task HandlePetAbandon(PetCommandAction request, Pet pet)
    {
        var waitingSpan = _sentry.StartSpanOnCurrentTransaction("Wait for confirmation");
        if (await InteractivityHelper.GetConfirmation(request.Responder, request.Member, "Pet Release"))
        {
            waitingSpan.Finish();
            
            var removeSpan = _sentry.StartSpanOnCurrentTransaction("Remove Pet");
            await _cache.Pets.RemovePet(request.Member.Id, pet.RowId);
            removeSpan.Finish();
            
            if (_cache.Pets.TryGetUsersPets(request.Member.Id, out var allPets))
            {
                var movePetsSpan = _sentry.StartSpanOnCurrentTransaction("Move Pet Priority", "Move pets up in priority to fill gap.");
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
                movePetsSpan.Finish();
            }

            request.Responder.Respond(PetMessages.GetAbandonSuccessMessage(pet));
        }
    }
}