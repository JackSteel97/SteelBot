using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Sentry;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.Helpers;
using SteelBot.Helpers.Constants;
using SteelBot.Helpers.Extensions;
using SteelBot.Helpers.Levelling;
using SteelBot.Helpers.Sentry;
using SteelBot.Services;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Services;

public class PetTreatingService
{
    private readonly DataCache _cache;
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly IHub _sentry;

    public PetTreatingService(DataCache cache, ErrorHandlingService errorHandlingService, IHub sentry)
    {
        _cache = cache;
        _errorHandlingService = errorHandlingService;
        _sentry = sentry;
    }

    public async Task Treat(CommandContext context)
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
                (pet) => Interactions.Pets.Treat(pet.Pet.RowId, pet.Pet.GetName()));
            pagesSpan.Finish();
            transaction.Finish();

            (string resultId, _) = await InteractivityHelper.SendPaginatedMessageWithComponentsAsync(context.Channel, context.User, pages);
            var responseTransaction = _sentry.StartNewConfiguredTransaction(nameof(PetTreatingService), "Handle Treat Response");
            if (!string.IsNullOrWhiteSpace(resultId))
            {
                var handleSpan = responseTransaction.StartChild("Handle Treat");
                // Figure out which pet they want to manage.
                if (PetShared.TryGetPetIdFromComponentId(resultId, out long petId))
                {
                    double treatBonus = PetShared.GetBonusValue(availablePets, BonusType.PetTreatXp);
                    await HandleTreatGiven(context, petId, treatBonus);
                }
                handleSpan.Finish();
            }
        }
        else
        {
            context.RespondAsync(PetMessages.GetNoPetsAvailableMessage(), mention: true).FireAndForget(_errorHandlingService);
        }
    }

    private async Task HandleTreatGiven(CommandContext context, long petId, double petTreatXpBonus)
    {
        var transaction = _sentry.GetCurrentTransaction();
        if (_cache.Pets.TryGetPet(context.Member.Id, petId, out var pet) && _cache.Users.TryGetUser(context.Guild.Id, context.Member.Id, out var user))
        {
            var xpMathsSpan = transaction.StartChild("Calculate Treat Xp");
            double xpRequiredForNextLevel = LevellingMaths.PetXpForLevel(pet.CurrentLevel + 1, pet.Rarity);
            double xpRequiredForThisLevel = LevellingMaths.PetXpForLevel(pet.CurrentLevel, pet.Rarity);
            double xpRequiredToLevel = xpRequiredForNextLevel - xpRequiredForThisLevel;
            int upperBound = Math.Max(101, (int)Math.Round(xpRequiredToLevel));
            int xpGain = RandomNumberGenerator.GetInt32(100, upperBound);
            pet.EarnedXp += xpGain;
            xpMathsSpan.Finish();

            var xpChangedSpan = transaction.StartChild("Pet Xp Changed");
            var changes = new StringBuilder();
            bool levelledUp = PetShared.PetXpChanged(pet, changes, user.CurrentLevel, out bool shouldPingOwner);
            xpChangedSpan.Finish();
            await _cache.Pets.UpdatePet(pet);
            context.Channel.SendMessageAsync(PetMessages.GetPetTreatedMessage(pet, xpGain)).FireAndForget(_errorHandlingService);
            if (levelledUp && _cache.Guilds.TryGetGuild(context.Guild.Id, out var guild))
            {
                PetShared.SendPetLevelledUpMessage(changes, guild, context.Guild, context.Member.Id, shouldPingOwner).FireAndForget(_errorHandlingService);
            }
        }
    }
}