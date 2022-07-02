using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.Database.Models.Pets;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Generation;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.Helpers;
using SteelBot.Helpers.Constants;
using SteelBot.Helpers.Extensions;
using SteelBot.Helpers.Sentry;
using SteelBot.Services;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using User = SteelBot.Database.Models.Users.User;

namespace SteelBot.DiscordModules.Pets.Services;

public class PetBefriendingService
{
    private readonly DataCache _cache;
    private readonly PetFactory _petFactory;
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly ILogger<PetBefriendingService> _logger;
    private readonly IHub _sentry;

    public PetBefriendingService(DataCache cache, PetFactory petFactory, ErrorHandlingService errorHandlingService, ILogger<PetBefriendingService> logger, IHub sentry)
    {
        _cache = cache;
        _petFactory = petFactory;
        _errorHandlingService = errorHandlingService;
        _logger = logger;
        _sentry = sentry;
    }

    public async Task Search(CommandContext context)
    {
        var transaction = _sentry.GetCurrentTransaction();
        bool isReplacementBefriend = false;
        if (!HasSpaceForAnotherPet(context.Member))
        {
            isReplacementBefriend = CanReplaceToBefriend(context.Member);
            if (!isReplacementBefriend)
            {
                var responseSpan = transaction.StartChild("Slots Full Response", "Too Many slots to replace befriend");
                _logger.LogInformation("User {UserId} cannot search for a new pet because their pet slots are already full", context.Member.Id);
                await context.RespondAsync(EmbedGenerator.Warning($"You don't have enough room for another pet{Environment.NewLine}Use `Pet Manage` to release one of your existing pets to make room"), mention: true);
                responseSpan.Finish();
                return;
            }
            _logger.LogInformation("User {UserId} is performing a replacement search", context.Member.Id);
        }

        if (!SearchSuccess(context.Member))
        {
            var responseSpan = transaction.StartChild("Nothing Found Response");
            _logger.LogInformation("User {UserId} failed to find anything when searching for a new pet", context.Member.Id);
            await context.RespondAsync(EmbedGenerator.Info($"You didn't find anything this time!{Environment.NewLine}Try again later", "Nothing Found"), mention: true);
            responseSpan.Finish();
            return;
        }

        (bool befriendAttempt, var pet, var interaction) = await HandleInitialSearchSuccess(context, isReplacementBefriend);
        bool newPet = false;
        if (befriendAttempt)
        {
            newPet = await HandleBefriendAttempt(context, pet, interaction);
        }

        if (newPet)
        {
            await context.RespondAsync(PetMessages.GetPetOwnedSuccessMessage(context.Member, pet), mention: true);
        }
    }

    private async Task<(bool befriendAttempt, Pet pet, DiscordInteraction interaction)> HandleInitialSearchSuccess(CommandContext context, bool mustReplace)
    {
        var transaction = _sentry.GetCurrentTransaction();
        var buildPetDisplaySpan = transaction.StartChild("Build Pet Display");
        bool befriendAttempt = false;
        DiscordInteraction interaction = null;
        _cache.Users.TryGetUser(context.Guild.Id, context.Member.Id, out var user);
        var foundPet = _petFactory.Generate(user?.CurrentLevel ?? 0);
        var initialPetDisplay = PetDisplayHelpers.GetPetDisplayEmbed(foundPet, includeName: false);

        const string replacementWarning = "Warning: If you befriend this pet you must choose an existing one to release.";
        var initialResponseBuilder = new DiscordMessageBuilder()
            .WithContent($"You found a new potential friend!{(mustReplace ? Environment.NewLine + replacementWarning : "")}")
            .WithEmbed(initialPetDisplay)
            .AddComponents(Interactions.Pets.Befriend, Interactions.Pets.Leave);

        _logger.LogInformation("Sending pet found message to User {UserId} in Guild {GuildId}", context.User.Id, context.Guild.Id);
        buildPetDisplaySpan.Finish();
        transaction.Finish(SpanStatus.Ok);
        var message = await context.RespondAsync(initialResponseBuilder, mention: true);
        var result = await message.WaitForButtonAsync(context.Member);
        _sentry.StartNewConfiguredTransaction(nameof(PetBefriendingService), "Search Success Response");

        if (!result.TimedOut)
        {
            initialResponseBuilder.ClearComponents();
            message.ModifyAsync(initialResponseBuilder).FireAndForget(_errorHandlingService);
            befriendAttempt = result.Result.Id == InteractionIds.Pets.Befriend;
            interaction = result.Result.Interaction;
        }
        else
        {
            _logger.LogInformation("Pet found message timed out waiting for a user response from User {UserId} in Guild {GuildId}", context.User.Id, context.Guild.Id);
            message.DeleteAsync().FireAndForget(_errorHandlingService);
            context.RespondAsync(PetMessages.GetPetRanAwayMessage(foundPet)).FireAndForget(_errorHandlingService);
        }

        return (befriendAttempt, foundPet, interaction);
    }

    private async Task<bool> HandleBefriendAttempt(CommandContext context, Pet pet, DiscordInteraction interaction)
    {
        _logger.LogInformation("User {UserId} in Guild {GuildId} is attempting to befriend a {Rarity} pet", context.User.Id, context.Guild.Id, pet.Rarity);
        bool befriendSuccess = false;
        bool hasSpace = HasSpaceForAnotherPet(context.Member);
        bool replacementBefriend = !hasSpace && CanReplaceToBefriend(context.Member);

        if ((hasSpace || replacementBefriend) && BefriendSuccess(context.Member, pet))
        {
            if (replacementBefriend)
            {
                (befriendSuccess, interaction) = await HandleReplacingBefriend(context, pet);
            }
            else
            {
                await HandleNonReplacingBefriend(context, pet);
                befriendSuccess = true;
            }

            if (befriendSuccess)
            {
                _logger.LogInformation("User {UserId} in Guild {GuildId} successfully befriended a {Rarity} pet with Id {PetId}", context.User.Id, context.Guild.Id, pet.Rarity, pet.RowId);

                if (PetCorrupted() && _cache.Users.TryGetUser(context.Guild.Id, context.Member.Id, out var ownerUser))
                {
                    var transaction = _sentry.GetCurrentTransaction();
                    var corruptSpan = transaction.StartChild("Corrupt Pet");
                    _logger.LogInformation("Pet {PetId} became corrupted after being befriended", pet.RowId);
                    pet = PetBonusFactory.Corrupt(pet, ownerUser.CurrentLevel);
                    await _cache.Pets.UpdatePet(pet);
                    var response = PetMessages.GetPetCorruptedMessage(pet).WithReply(context.Message.Id, mention: true);
                    context.Channel.SendMessageAsync(response).FireAndForget(_errorHandlingService);
                    corruptSpan.Finish();
                }

                await PetModals.NamePet(interaction, pet);
            }
        }
        else
        {
            var response = PetMessages.GetBefriendFailedMessage(pet).WithReply(context.Message.Id, mention: true);
            context.Channel.SendMessageAsync(response).FireAndForget(_errorHandlingService);
        }
        return befriendSuccess;
    }

    private async Task<(bool befriendSuccess, DiscordInteraction interaction)> HandleReplacingBefriend(CommandContext context, Pet newPet)
    {
        DiscordInteraction interaction = null;
        if (_cache.Users.TryGetUser(context.Guild.Id, context.User.Id, out var user)
           && _cache.Pets.TryGetUsersPets(context.User.Id, out var allPets))
        {
            var availablePets = PetShared.GetAvailablePets(user, allPets, out var disabledPets);
            var combinedPets = PetShared.Recombine(availablePets, disabledPets);

            var baseEmbed = PetShared.GetOwnedPetsBaseEmbed(user, availablePets, disabledPets.Count > 0);

            int maxCapacity = PetShared.GetPetCapacity(user, allPets);
            int baseCapacity = PetShared.GetBasePetCapacity(user);

            var pages = PaginationHelper.GenerateEmbedPages(baseEmbed, combinedPets, 10,
                   (builder, pet) => PetShared.AppendPetDisplayShort(builder, pet.Pet, pet.Active, baseCapacity, maxCapacity),
                   (pet) => Interactions.Pets.Replace(pet.Pet.RowId, pet.Pet.GetName()));

            _sentry.GetCurrentTransaction()?.Finish();
            (string resultId, interaction) = await InteractivityHelper.SendPaginatedMessageWithComponentsAsync(context.Channel, context.User, pages);
            _sentry.StartNewConfiguredTransaction(nameof(PetBefriendingService), "Replacement Befriend Response");

            // Figure out which pet they want to replace.
            if (!string.IsNullOrWhiteSpace(resultId) &&
                PetShared.TryGetPetIdFromComponentId(resultId, out long petId))
            {
                await ReplacePetWith(context, petId, newPet);
            }
            else
            {
                context.Channel.SendMessageAsync(PetMessages.GetPetRanAwayMessage(newPet)).FireAndForget(_errorHandlingService);
            }
        }
        // Successfully replaced?
        return (newPet.RowId != default, interaction);
    }

    private async Task ReplacePetWith(CommandContext context, long petId, Pet newPet)
    {
        var transaction = _sentry.GetCurrentTransaction();
        var removeSpan = transaction.StartChild("Remove Pet");
        if (_cache.Pets.TryGetPet(context.Member.Id, petId, out var petToReplace))
        {
            int priority = petToReplace.Priority;
            await _cache.Pets.RemovePet(context.Member.Id, petId);
            newPet.Priority = priority;
            removeSpan.Finish();
            await AddPet(context.Member.Id, newPet);
        }
    }

    private async Task HandleNonReplacingBefriend(CommandContext context, Pet pet)
    {
        _cache.Pets.TryGetUsersPetsCount(context.Member.Id, out int numberOfOwnedPets);
        pet.Priority = numberOfOwnedPets;
        await AddPet(context.Member.Id, pet);
    }

    private async Task AddPet(ulong userId, Pet pet)
    {
        var transaction = _sentry.GetCurrentTransaction();
        var addSpan = transaction.StartChild("Add Pet");
        pet.OwnerDiscordId = userId;
        pet.RowId = await _cache.Pets.InsertPet(pet);
        addSpan.Finish();
    }

    private bool SearchSuccess(DiscordMember userSearching)
    {
        double probability = GetSearchSuccessProbability(userSearching);
        _logger.LogDebug("Search success probability for User {UserId} is {Probability}", userSearching.Id, probability);
        return MathsHelper.TrueWithProbability(probability);
    }

    private double GetSearchSuccessProbability(DiscordMember userSearching)
    {
        var transaction = _sentry.GetCurrentTransaction();
        var successProbabilitySpan = transaction.StartChild(nameof(GetSearchSuccessProbability));
        int ownedPetCount = 0;
        double bonusMultiplier = 1;
        if (_cache.Users.TryGetUser(userSearching.Guild.Id, userSearching.Id, out var user)
            && _cache.Pets.TryGetUsersPets(userSearching.Id, out var ownedPets))
        {
            var activePets = PetShared.GetAvailablePets(user, ownedPets, out _);
            ownedPetCount = ownedPets.Count;

            bonusMultiplier = PetShared.GetBonusValue(activePets, BonusType.SearchSuccessRate);
        }

        double probability = 2D / ownedPetCount * bonusMultiplier;
        double finalProbability = Math.Min(1, probability);
        successProbabilitySpan.Finish();
        return finalProbability;
    }

    private bool HasSpaceForAnotherPet(DiscordMember user)
    {
        var transaction = _sentry.GetCurrentTransaction();
        var spaceSpan = transaction.StartChild(nameof(HasSpaceForAnotherPet));

        (int capacity, int allPetsCount) = GetCapacityAndAllPetsCount(user);
        bool hasSpace = allPetsCount < capacity;

        spaceSpan.Finish();
        return hasSpace;
    }

    private (int capacity, int allPetsCount) GetCapacityAndAllPetsCount(DiscordMember user)
    {
        int capacity = 0;
        int allPetsCount = 0;
        if (_cache.Users.TryGetUser(user.Guild.Id, user.Id, out var dbUser))
        {
            _cache.Pets.TryGetUsersPets(user.Id, out var allPets);
            capacity = PetShared.GetPetCapacity(dbUser, allPets);
            allPetsCount = allPets.Count;
        }
        return (capacity, allPetsCount);
    }

    private bool CanReplaceToBefriend(DiscordMember user)
    {
        var transaction = _sentry.GetCurrentTransaction();
        var canReplaceSpan = transaction.StartChild(nameof(CanReplaceToBefriend));

        (int capacity, int allPetsCount) = GetCapacityAndAllPetsCount(user);
        bool canReplace = allPetsCount < capacity + 1;

        canReplaceSpan.Finish();
        return canReplace;
    }

    private bool BefriendSuccess(DiscordMember user, Pet target)
    {
        if (_cache.Users.TryGetUser(user.Guild.Id, user.Id, out var dbUser))
        {
            double probability = GetBefriendSuccessProbability(dbUser, target);
            _logger.LogDebug("Befriend success probability for User {UserId} and Pet Rarity {Rarity} is {Probability}", user.Id, target.Rarity, probability);
            return MathsHelper.TrueWithProbability(probability);
        }
        return false;
    }

    private double GetBefriendSuccessProbability(User user, Pet target)
    {
        var transaction = _sentry.GetCurrentTransaction();
        var befriendSuccessSpan = transaction.StartChild(nameof(GetBefriendSuccessProbability));
        const double baseRate = 0.1;
        int ownedPetCount = 0;
        double bonusMultiplier = 1;
        double petCapacity;

        var dependantsSpan = befriendSuccessSpan.StartChild("Calculate Dependencies");
        if (_cache.Pets.TryGetUsersPets(user.DiscordId, out var allPets))
        {
            var capacitySpan = dependantsSpan.StartChild("Get Capacities");
            var activePets = PetShared.GetAvailablePets(user, allPets, out _);
            ownedPetCount = allPets.Count;
            petCapacity = PetShared.GetPetCapacity(user, allPets);
            capacitySpan.Finish();

            var bonusSpan = dependantsSpan.StartChild("Get Befriend Bonus");
            bonusMultiplier = PetShared.GetBonusValue(activePets, BonusType.BefriendSuccessRate);
            bonusSpan.Finish();
        }
        else
        {
            var capacitySpan = dependantsSpan.StartChild("Get Base Capacity", "User has no pets");
            petCapacity = PetShared.GetBasePetCapacity(user);
            capacitySpan.Finish();
        }
        dependantsSpan.Finish();

        var raritySpan = befriendSuccessSpan.StartChild("Get Random Rarity Modifier");
        int rarityModifier = RandomNumberGenerator.GetInt32((int)target.Rarity + 1);
        raritySpan.Finish();

        double currentRate = baseRate + ((petCapacity - ownedPetCount) / (petCapacity + rarityModifier));
        double finalRate = currentRate * bonusMultiplier;
        befriendSuccessSpan.Finish();
        return finalRate;
    }

    private static bool PetCorrupted() => MathsHelper.TrueWithProbability(0.001);
}