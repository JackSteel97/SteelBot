using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.Channels.Pets;
using SteelBot.Database.Models.Pets;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.Helpers;
using SteelBot.Helpers.Constants;
using SteelBot.Helpers.Extensions;
using SteelBot.Helpers.Sentry;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using User = SteelBot.Database.Models.Users.User;

namespace SteelBot.DiscordModules.Pets.Services;

public class PetTreatingService
{
    private readonly DataCache _cache;
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly IHub _sentry;
    private readonly ILogger<PetTreatingService> _logger;

    public PetTreatingService(DataCache cache, ErrorHandlingService errorHandlingService, IHub sentry, ILogger<PetTreatingService> logger)
    {
        _cache = cache;
        _errorHandlingService = errorHandlingService;
        _sentry = sentry;
        _logger = logger;
    }

    public async Task Treat(PetCommandAction request)
    {
        if (request.Action != PetCommandActionType.Treat) throw new ArgumentException($"Unexpected action type sent to {nameof(Treat)}");
        var transaction = _sentry.GetCurrentTransaction();
        await TreatCore(request, transaction);
    }

    private async Task TreatCore(PetCommandAction request, ITransaction transaction)
    {
        if (!_cache.Users.TryGetUser(request.Guild.Id, request.Member.Id, out var user)
            || !_cache.Pets.TryGetUsersPets(request.Member.Id, out var allPets))
        {
            request.Responder.Respond(PetMessages.GetNoPetsAvailableMessage());
            return;
        }

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
            (pet) => Interactions.Pets.Treat(pet.Pet.RowId, pet.Pet.GetName()).Disable(!pet.Active));
        pagesSpan.Finish();
        transaction.Finish();

        (string resultId, _) = await request.Responder.RespondPaginatedWithComponents(pages);
        await HandleResponse(request, resultId, availablePets);
    }

    private async Task HandleResponse(PetCommandAction request, string resultId, List<Pet> availablePets)
    {
        var responseTransaction = _sentry.StartNewConfiguredTransaction(nameof(PetTreatingService), "Handle Treat Response");
        if (!string.IsNullOrWhiteSpace(resultId))
        {
            var handleSpan = responseTransaction.StartChild("Handle Treat");
            // Figure out which pet they want to manage.
            if (PetShared.TryGetPetIdFromComponentId(resultId, out long petId))
            {
                double treatBonus = PetShared.GetBonusValue(availablePets, BonusType.PetTreatXp);
                await HandleTreatGiven(request, petId, treatBonus);
            }
            handleSpan.Finish();
        }
    }

    private async Task HandleTreatGiven(PetCommandAction request, long petId, double petTreatXpBonus)
    {
        var transaction = _sentry.GetCurrentTransaction();
        if (_cache.Pets.TryGetPet(request.Member.Id, petId, out var pet)
            && _cache.Users.TryGetUser(request.Guild.Id, request.Member.Id, out var user))
        {
            await HandleTreatGivenCore(request, pet, user, petTreatXpBonus, transaction);
        }
    }

    private async Task HandleTreatGivenCore(PetCommandAction request, Pet pet, User user, double petTreatXpBonus, ITransaction transaction)
    {
        var xpMathsSpan = transaction.StartChild("Calculate Treat Xp");
        int xpGain;
        using (_logger.BeginScope("Calculating Treat XP for User {UserId}, Pet {PetId} with Rarity {Rarity}", user.DiscordId, pet.RowId, pet.Rarity))
        {
            xpGain = (int)PetMaths.CalculateTreatXp(pet.CurrentLevel, pet.Rarity, petTreatXpBonus, _logger);
        }
        pet.EarnedXp += xpGain;
        xpMathsSpan.Finish();

        var xpChangedSpan = transaction.StartChild("Pet Xp Changed");
        var changes = new StringBuilder();
        bool levelledUp = PetShared.PetXpChanged(pet, changes, user.CurrentLevel, out bool shouldPingOwner);
        xpChangedSpan.Finish();
        await _cache.Pets.UpdatePet(pet);
        request.Responder.Respond(PetMessages.GetPetTreatedMessage(pet, (int)xpGain));
        if (levelledUp && _cache.Guilds.TryGetGuild(request.Guild.Id, out var guild))
        {
            PetShared.SendPetLevelledUpMessage(changes, guild, request.Guild, user, shouldPingOwner).FireAndForget(_errorHandlingService);
        }
    }
}