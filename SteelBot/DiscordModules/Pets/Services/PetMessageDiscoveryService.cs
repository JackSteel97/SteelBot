using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using SteelBot.Channels.Pets;
using SteelBot.Database.Models.Pets;
using SteelBot.Database.Models.Users;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets.Generation;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.Helpers;
using SteelBot.Services;
using System;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Services;

public class PetMessageDiscoveryService
{
    private readonly DataCache _cache;
    private readonly ILogger<PetMessageDiscoveryService> _logger;
    private readonly LevelMessageSender _levelMessageSender;
    private readonly PetFactory _petFactory;

    public PetMessageDiscoveryService(DataCache cache, ILogger<PetMessageDiscoveryService> logger, LevelMessageSender levelMessageSender, PetFactory petFactory)
    {
        _cache = cache;
        _logger = logger;
        _levelMessageSender = levelMessageSender;
        _petFactory = petFactory;
    }

    public Task RunCheck(PetCommandAction request)
    {
        if (request.Action != PetCommandActionType.MessageBasedPetDiscovery) throw new ArgumentException($"Unexpected action type sent to {nameof(RunCheck)}");
        return RunCheckCore(request);
    }

    private async Task RunCheckCore(PetCommandAction request)
    {
        if (!_cache.Users.TryGetUser(request.Guild.Id, request.Member.Id, out var user))
        {
            _logger.LogWarning("Could not get user, skipping checks");
            return;
        }

        if (!SummonedAPet(request.Member, request.TriggerWord))
        {
            _logger.LogInformation("Did not pass check for summoning a pet");
            return;
        }

        var pet = await HandlePetSummoned(user, request.TriggerWord);
        _levelMessageSender.SendPetSummonedMessage(request.Guild, request.Member, pet);
    }

    private async Task<Pet> HandlePetSummoned(User user, string triggerWord)
    {
        var foundPet = _petFactory.Generate(user.CurrentLevel);
        foundPet.Name = triggerWord;
        _cache.Pets.TryGetUsersPetsCount(user.DiscordId, out int numberOfOwnedPets);
        foundPet.Priority = numberOfOwnedPets;
        foundPet.OwnerDiscordId = user.DiscordId;
        foundPet.RowId = await _cache.Pets.InsertPet(foundPet);
        return foundPet;
    }

    private bool SummonedAPet(DiscordMember member, string triggerWord)
    {
        if (!PetSpaceHelper.HasSpaceForAnotherPet(member, _cache.Users, _cache.Pets))
        {
            return false;
        }

        var petSpace = PetSpaceHelper.GetCapacityAndAllPetsCount(member, _cache.Users, _cache.Pets);
        double probabilityMultiplierFromPetSpace = 1 - ((double)petSpace.allPetsCount / petSpace.capacity);
        double probabilityAdderFromMessage = triggerWord.Length / 100d;

        double finalProbability = (0.5d * probabilityMultiplierFromPetSpace) + probabilityAdderFromMessage;

        return MathsHelper.TrueWithProbability(finalProbability);
    }
}