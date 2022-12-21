using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using SteelBot.Channels.Pets;
using SteelBot.Database.Models.Pets;
using SteelBot.Database.Models.Users;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Generation;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.Helpers;
using SteelBot.Helpers.Constants;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Services;

public class PetBefriendingService
{
    private readonly DataCache _cache;
    private readonly ILogger<PetBefriendingService> _logger;

    public PetBefriendingService(DataCache cache, ILogger<PetBefriendingService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task Befriend(PetCommandAction request, Pet foundPet, DiscordInteraction interaction)
    {
        _logger.LogInformation("User {UserId} in Guild {GuildId} is attempting to befriend a {Rarity} pet", request.Member.Id, request.Guild.Id, foundPet.Rarity);

        bool hasSpace = PetSpaceHelper.HasSpaceForAnotherPet(request.Member, _cache.Users, _cache.Pets);
        bool isReplacementBefriend = !hasSpace && PetSpaceHelper.CanReplaceToBefriend(request.Member, _cache.Users, _cache.Pets);

        await BefriendCore(request, foundPet, hasSpace, isReplacementBefriend, interaction);
    }

    private async Task BefriendCore(PetCommandAction request, Pet foundPet, bool hasSpace, bool isReplacementBefriend, DiscordInteraction interaction)
    {
        if ((!hasSpace && !isReplacementBefriend) || !BefriendSuccess(request.Member, foundPet))
        {
            request.Responder.Respond(PetMessages.GetBefriendFailedMessage(foundPet));
            return;
        }

        bool befriendSuccess = false;
        if (isReplacementBefriend)
        {
            (befriendSuccess, interaction) = await HandleReplacingBefriend(request, foundPet);
        }
        else
        {
            await HandleNonReplacingBefriend(request.Member, foundPet);
            befriendSuccess = true;
        }

        if (befriendSuccess)
        {
            _logger.LogInformation("User {UserId} in Guild {GuildId} successfully befriended a {Rarity} pet with Id {PetId}", request.Member.Id, request.Guild.Id, foundPet.Rarity, foundPet.RowId);
            foundPet = await HandlePetCorruptionChance(request, foundPet);

            await PetModals.NamePet(interaction, foundPet);
        }
        else
        {
            request.Responder.Respond(PetMessages.GetBefriendFailedMessage(foundPet));
        }
    }

    private async ValueTask<Pet> HandlePetCorruptionChance(PetCommandAction request, Pet pet)
    {
        if (!PetCorrupted() || !_cache.Users.TryGetUser(request.Guild.Id, request.Member.Id, out var ownerUser)) return pet;

        _logger.LogInformation("Pet {PetId} became corrupted after being befriended", pet.RowId);
        pet = PetBonusFactory.Corrupt(pet, ownerUser.CurrentLevel);
        await _cache.Pets.UpdatePet(pet);
        request.Responder.Respond(PetMessages.GetPetCorruptedMessage(pet));

        return pet;
    }

    private async Task<(bool befriendSuccess, DiscordInteraction interaction)> HandleReplacingBefriend(PetCommandAction request, Pet newPet)
    {
        DiscordInteraction interaction = null;
        if (!_cache.Users.TryGetUser(request.Guild.Id, request.Member.Id, out var user)
            || !_cache.Pets.TryGetUsersPets(request.Member.Id, out var allPets))
            return (false, null);
        var availablePets = PetShared.GetAvailablePets(user, allPets, out var disabledPets);
        var combinedPets = PetShared.Recombine(availablePets, disabledPets);

        var baseEmbed = PetShared.GetOwnedPetsBaseEmbed(user, availablePets, disabledPets.Count > 0);

        int maxCapacity = PetShared.GetPetCapacity(user, allPets);
        int baseCapacity = PetShared.GetBasePetCapacity(user);

        var pages = PaginationHelper.GenerateEmbedPages(baseEmbed, combinedPets, 10,
            (builder, pet) => PetShared.AppendPetDisplayShort(builder, pet.Pet, pet.Active, baseCapacity, maxCapacity),
            pet => Interactions.Pets.Replace(pet.Pet.RowId, pet.Pet.GetName()));

        (string resultId, interaction) = await request.Responder.RespondPaginatedWithComponents(pages);

        // Figure out which pet they want to replace.
        if (!string.IsNullOrWhiteSpace(resultId) &&
            PetShared.TryGetPetIdFromComponentId(resultId, out long petId))
            await ReplacePetWith(request.Member, petId, newPet);
        else
            request.Responder.Respond(PetMessages.GetPetRanAwayMessage(newPet));

        // Successfully replaced?
        return (newPet.RowId != default, interaction);
    }

    private async Task ReplacePetWith(DiscordMember member, long petId, Pet newPet)
    {
        if (_cache.Pets.TryGetPet(member.Id, petId, out var petToReplace))
        {
            int priority = petToReplace.Priority;
            await _cache.Pets.RemovePet(member.Id, petId);
            newPet.Priority = priority;
            await AddPet(member.Id, newPet);
        }
    }

    private async Task HandleNonReplacingBefriend(DiscordMember member, Pet pet)
    {
        _cache.Pets.TryGetUsersPetsCount(member.Id, out int numberOfOwnedPets);
        pet.Priority = numberOfOwnedPets;
        await AddPet(member.Id, pet);
    }

    private async Task AddPet(ulong userId, Pet pet)
    {
        pet.OwnerDiscordId = userId;
        pet.RowId = await _cache.Pets.InsertPet(pet);
    }

    private bool BefriendSuccess(DiscordMember member, Pet target)
    {
        if (!_cache.Users.TryGetUser(member.Guild.Id, member.Id, out var dbUser)) return false;

        double probability = GetBefriendSuccessProbability(dbUser, target);
        _logger.LogInformation("Befriend success probability for User {UserId} and Pet Rarity {Rarity} is {Probability}", member.Id, target.Rarity, probability);
        return MathsHelper.TrueWithProbability(probability);
    }

    private double GetBefriendSuccessProbability(User user, Pet target)
    {
        const double baseRate = 0.1;
        int ownedPetCount = 0;
        double bonusMultiplier = 1;
        double petCapacity;

        if (_cache.Pets.TryGetUsersPets(user.DiscordId, out var allPets))
        {
            var activePets = PetShared.GetAvailablePets(user, allPets, out _);
            ownedPetCount = allPets.Count;
            petCapacity = PetShared.GetPetCapacity(user, allPets);

            bonusMultiplier = PetShared.GetBonusValue(activePets, BonusType.BefriendSuccessRate);
        }
        else
        {
            petCapacity = PetShared.GetBasePetCapacity(user);
        }

        int rarityModifier = RandomNumberGenerator.GetInt32((int)target.Rarity + 1);

        double currentRate = baseRate + ((petCapacity - ownedPetCount) / (petCapacity + rarityModifier));
        double finalRate = currentRate * bonusMultiplier;
        return finalRate;
    }

    private static bool PetCorrupted() => MathsHelper.TrueWithProbability(0.001);
}