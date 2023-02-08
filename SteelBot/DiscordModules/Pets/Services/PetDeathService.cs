using Microsoft.Extensions.Logging;
using SteelBot.Channels.Pets;
using SteelBot.Database.Models.Pets;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.Helpers;
using SteelBot.Services;
using System;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Services;

public class PetDeathService
{
    private readonly DataCache _cache;
    private readonly ILogger<PetDeathService> _logger;
    private readonly LevelMessageSender _levelMessageSender;

    public PetDeathService(DataCache cache, ILogger<PetDeathService> logger, LevelMessageSender levelMessageSender)
    {
        _cache = cache;
        _logger = logger;
        _levelMessageSender = levelMessageSender;
    }

    public Task RunCheck(PetCommandAction request)
    {
        if (request.Action != PetCommandActionType.CheckForDeath) throw new ArgumentException($"Unexpected action type sent to {nameof(RunCheck)}");
        return RunCheckCore(request);
    }

    private async Task RunCheckCore(PetCommandAction request)
    {
        if (!_cache.Users.TryGetUser(request.Guild.Id, request.Member.Id, out var user)
            || !_cache.Pets.TryGetUsersPets(request.Member.Id, out var allPets))
        {
            _logger.LogWarning("Could not get user's pets, skipping checks");
            return;
        }

        foreach (var pet in allPets)
        {
            bool died = await CheckPet(pet);
            if (died)
            {
                _levelMessageSender.SendPetDiedMessage(request.Guild, request.Member, pet);
            }
        }
    }

    private async Task<bool> CheckPet(Pet pet)
    {
        double chanceToDie = ChanceToDie(pet);
        if (!MathsHelper.TrueWithProbability(chanceToDie)) return false;
        
        _logger.LogInformation("Killing pet {PetId}, a {PetDescription}, with probability {ChanceToDie}", pet.RowId, pet.ShortDescription, chanceToDie);
        await KillPet(pet);
        return true;
    }

    private async Task KillPet(Pet pet)
    {
        pet.IsDead = true;
        await _cache.Pets.UpdatePet(pet);
    }

    private double ChanceToDie(Pet pet)
    {
        if (pet.FoundAt.Date == DateTime.Today) return 0;

        double lifeProgress = pet.Age / pet.Species.GetMaxAge();
        double baseMultiplier = 1D / (pet.Rarity.GetStartingBonusCount() + (int)pet.Rarity + (int)pet.Size);

        double chanceToDie = lifeProgress * baseMultiplier;
        _logger.LogDebug("Chance to die is {ChanceToDie}, Life Progress is {LifeProgress}, Base Multiplier is {BaseMultiplier}", chanceToDie, lifeProgress, baseMultiplier);
        return chanceToDie;
    }
}