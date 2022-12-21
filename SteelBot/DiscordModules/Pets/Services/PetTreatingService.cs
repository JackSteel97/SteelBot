using Microsoft.Extensions.Logging;
using SteelBot.Channels.Pets;
using SteelBot.Database.Models.Pets;
using SteelBot.Database.Models.Users;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.Helpers;
using SteelBot.Helpers.Constants;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Services;

public class PetTreatingService
{
    private readonly DataCache _cache;
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly ILogger<PetTreatingService> _logger;

    public PetTreatingService(DataCache cache, ErrorHandlingService errorHandlingService, ILogger<PetTreatingService> logger)
    {
        _cache = cache;
        _errorHandlingService = errorHandlingService;
        _logger = logger;
    }

    public async Task Treat(PetCommandAction request)
    {
        if (request.Action != PetCommandActionType.Treat) throw new ArgumentException($"Unexpected action type sent to {nameof(Treat)}");
        await TreatCore(request);
    }

    private async Task TreatCore(PetCommandAction request)
    {
        if (!_cache.Users.TryGetUser(request.Guild.Id, request.Member.Id, out var user)
            || !_cache.Pets.TryGetUsersPets(request.Member.Id, out var allPets))
        {
            request.Responder.Respond(PetMessages.GetNoPetsAvailableMessage());
            return;
        }

        var availablePets = PetShared.GetAvailablePets(user, allPets, out var disabledPets);
        var combinedPets = PetShared.Recombine(availablePets, disabledPets);

        var baseEmbed = PetShared.GetOwnedPetsBaseEmbed(user, availablePets, disabledPets.Count > 0);

        int maxCapacity = PetShared.GetPetCapacity(user, allPets);
        int baseCapacity = PetShared.GetBasePetCapacity(user);

        var pages = PaginationHelper.GenerateEmbedPages(baseEmbed, combinedPets, 10,
            (builder, pet) => PetShared.AppendPetDisplayShort(builder, pet.Pet, pet.Active, baseCapacity, maxCapacity),
            (pet) => Interactions.Pets.Treat(pet.Pet.RowId, pet.Pet.GetName()).Disable(!pet.Active));

        (string resultId, _) = await request.Responder.RespondPaginatedWithComponents(pages);
        await HandleResponse(request, resultId, availablePets);
    }

    private async Task HandleResponse(PetCommandAction request, string resultId, List<Pet> availablePets)
    {
        if (!string.IsNullOrWhiteSpace(resultId))
        {
            // Figure out which pet they want to manage.
            if (PetShared.TryGetPetIdFromComponentId(resultId, out long petId))
            {
                double treatBonus = PetShared.GetBonusValue(availablePets, BonusType.PetTreatXp);
                await HandleTreatGiven(request, petId, treatBonus);
            }
        }
    }

    private async Task HandleTreatGiven(PetCommandAction request, long petId, double petTreatXpBonus)
    {
        if (_cache.Pets.TryGetPet(request.Member.Id, petId, out var pet)
            && _cache.Users.TryGetUser(request.Guild.Id, request.Member.Id, out var user))
        {
            await HandleTreatGivenCore(request, pet, user, petTreatXpBonus);
        }
    }

    private async Task HandleTreatGivenCore(PetCommandAction request, Pet pet, User user, double petTreatXpBonus)
    {
        int xpGain;
        using (_logger.BeginScope("Calculating Treat XP for User {UserId}, Pet {PetId} with Rarity {Rarity}", user.DiscordId, pet.RowId, pet.Rarity))
        {
            xpGain = (int)PetMaths.CalculateTreatXp(pet.CurrentLevel, pet.Rarity, petTreatXpBonus, _logger);
        }
        pet.EarnedXp += xpGain;

        var changes = new StringBuilder();
        bool levelledUp = PetShared.PetXpChanged(pet, changes, user.CurrentLevel, out bool shouldPingOwner);
        await _cache.Pets.UpdatePet(pet);
        request.Responder.Respond(PetMessages.GetPetTreatedMessage(pet, (int)xpGain));
        if (levelledUp && _cache.Guilds.TryGetGuild(request.Guild.Id, out var guild))
        {
            PetShared.SendPetLevelledUpMessage(changes, guild, request.Guild, user, shouldPingOwner).FireAndForget(_errorHandlingService);
        }
    }
}