using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.Database.Models.Pets;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.DiscordModules.Pets.Models;
using SteelBot.DiscordModules.Pets.Services;
using SteelBot.Helpers;
using SteelBot.Helpers.Constants;
using SteelBot.Helpers.Extensions;
using SteelBot.Helpers.Sentry;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets;

public class PetsDataHelper
{
    private readonly DataCache _cache;
    private readonly PetBefriendingService _befriendingService;
    private readonly PetManagementService _managementService;
    private readonly PetTreatingService _treatingService;
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly ILogger<PetsDataHelper> _logger;
    private readonly IHub _sentry;

    public PetsDataHelper(DataCache cache,
        PetBefriendingService petBefriendingService,
        PetManagementService petManagementService,
        PetTreatingService petTreatingService,
        ErrorHandlingService errorHandlingService,
        ILogger<PetsDataHelper> logger,
        IHub sentry)
    {
        _cache = cache;
        _befriendingService = petBefriendingService;
        _managementService = petManagementService;
        _treatingService = petTreatingService;
        _errorHandlingService = errorHandlingService;
        _logger = logger;
        _sentry = sentry;
    }

    public async Task HandleSearch(CommandContext context)
    {
        try
        {
            await _befriendingService.Search(context);
        }
        catch (Exception e)
        {
            await _errorHandlingService.Log(e, nameof(HandleSearch));
        }
    }

    public async Task HandleManage(CommandContext context)
    {
        try
        {
            await _managementService.Manage(context);
        }
        catch (Exception e)
        {
            await _errorHandlingService.Log(e, nameof(HandleManage));
        }
    }

    public async Task HandleTreat(CommandContext context)
    {
        try
        {
            await _treatingService.Treat(context);
        }
        catch (Exception e)
        {
            await _errorHandlingService.Log(e, nameof(HandleTreat));
        }
    }

    public async Task HandleNamingPet(ModalSubmitEventArgs args)
    {
        string result = args.Values.Keys.FirstOrDefault();
        if (result != default && PetShared.TryGetPetIdFromComponentId(result, out long petId))
        {
            string newName = args.Values[result];
            if (!string.IsNullOrWhiteSpace(newName)
                && _cache.Pets.TryGetPet(args.Interaction.User.Id, petId, out var pet)
                && pet.OwnerDiscordId == args.Interaction.User.Id
                && newName != pet.Name)
            {
                _logger.LogInformation("User {UserId} is attempting to rename pet {PetId} from {OldName} to {NewName}", pet.OwnerDiscordId, pet.RowId, pet.Name, newName);
                pet.Name = newName;
                await _cache.Pets.UpdatePet(pet);

                args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder(PetMessages.GetNamingSuccessMessage(pet))).FireAndForget(_errorHandlingService);
                args.Handled = true;
                return;
            }
        }

        args.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate).FireAndForget(_errorHandlingService); ;
    }

    public async Task HandleMovingPet(ModalSubmitEventArgs args)
    {
        string result = args.Values.Keys.FirstOrDefault();
        if (result != default && PetShared.TryGetPetIdFromComponentId(result, out long petId))
        {
            string newPositionText = args.Values[result];

            if (!string.IsNullOrWhiteSpace(newPositionText)
                && int.TryParse(newPositionText, out int newPosition)
                && _cache.Pets.TryGetPet(args.Interaction.User.Id, petId, out var petBeingMoved)
                && newPosition - 1 != petBeingMoved.Priority)
            {
                int newPriority = newPosition - 1;
                _logger.LogInformation("User {UserId} is attempting to move Pet {PetId} from position {CurrentPriority} to {NewPriority}", petBeingMoved.OwnerDiscordId, petBeingMoved.RowId, petBeingMoved.Priority, newPriority);

                await _managementService.MovePetToPosition(petBeingMoved, newPriority);
            }
        }

        args.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate).FireAndForget(_errorHandlingService);
    }

    public async Task SendOwnedPetsDisplay(CommandContext context, DiscordMember target)
    {
        var transaction = _sentry.GetCurrentTransaction();
        if (_cache.Users.TryGetUser(target.Guild.Id, target.Id, out var user)
            && _cache.Pets.TryGetUsersPets(target.Id, out var pets))
        {
            var getPetsSpan = transaction.StartChild("Get Available Pets");
            var availablePets = PetShared.GetAvailablePets(user, pets, out var disabledPets);
            var combinedPets = PetShared.Recombine(availablePets, disabledPets);
            getPetsSpan.Finish();

            var messageBuilderSpan = transaction.StartChild("Build Message");
            var baseEmbed = PetShared.GetOwnedPetsBaseEmbed(user, pets, disabledPets.Count > 0, target.DisplayName)
                .WithThumbnail(target.AvatarUrl);

            if (combinedPets.Count == 0)
            {
                baseEmbed.WithDescription("You currently own no pets.");
                await context.RespondAsync(baseEmbed);
                return;
            }

            int maxCapacity = PetShared.GetPetCapacity(user, pets);
            int baseCapacity = PetShared.GetBasePetCapacity(user);
            var pages = PaginationHelper.GenerateEmbedPages(baseEmbed, combinedPets, 10, (builder, pet, _) => PetShared.AppendPetDisplayShort(builder, pet.Pet, pet.Active, baseCapacity, maxCapacity));
            var interactivity = context.Client.GetInteractivity();
            messageBuilderSpan.Finish();

            var displaySpan = transaction.StartChild("Display Paginated Message");
            await interactivity.SendPaginatedMessageAsync(context.Channel, context.User, pages);
            displaySpan.Finish();
        }
        else
        {
            context.RespondAsync(PetMessages.GetNoPetsAvailableMessage(), mention: true).FireAndForget(_errorHandlingService);
        }
    }

    public Task SendPetBonusesDisplay(CommandContext context, DiscordMember discordMember)
    {
        if (_cache.Users.TryGetUser(discordMember.Guild.Id, discordMember.Id, out var user)
            && _cache.Pets.TryGetUsersPets(discordMember.Id, out var pets))
        {
            var availablePets = PetShared.GetAvailablePets(user, pets, out var disabledPets);
            var combinedPets = PetShared.Recombine(availablePets, disabledPets);
            if (combinedPets.Count > 0)
            {
                int maxCapacity = PetShared.GetPetCapacity(user, pets);
                int baseCapacity = PetShared.GetBasePetCapacity(user);
                var pages = PetDisplayHelpers.GetPetBonusesSummary(combinedPets, discordMember.DisplayName, discordMember.AvatarUrl, baseCapacity, maxCapacity);

                var interactivity = context.Client.GetInteractivity();
                return interactivity.SendPaginatedMessageAsync(context.Channel, context.User, pages);
            }
        }
        return context.RespondAsync(PetMessages.GetNoPetsAvailableMessage(), mention: true);
    }

    public List<Pet> GetAvailablePets(ulong guildId, ulong userId, out List<Pet> disabledPets)
    {
        if (_cache.Users.TryGetUser(guildId, userId, out var user) && _cache.Pets.TryGetUsersPets(userId, out var pets))
        {
            return PetShared.GetAvailablePets(user, pets, out disabledPets);
        }
        disabledPets = new List<Pet>();
        return new List<Pet>();
    }

    public async Task PetXpUpdated(List<Pet> pets, DiscordGuild sourceGuild, int levelOfUser)
    {
        var changes = new StringBuilder();
        bool pingOwner = false;
        foreach (var pet in pets)
        {
            bool levelledUp = PetShared.PetXpChanged(pet, changes, levelOfUser, out bool shouldPingOwner);
            if (levelledUp)
            {
                if (shouldPingOwner)
                {
                    pingOwner = true;
                }
                changes.AppendLine();
            }
        }

        await _cache.Pets.UpdatePets(pets);

        if (changes.Length > 0 && sourceGuild != default && pets.Count > 0)
        {
            SendPetLevelledUpMessage(sourceGuild, changes, pets[0].OwnerDiscordId, pingOwner);
        }
    }

    public async Task Reorder(CommandContext context, string input)
    {
        if (_cache.Users.TryGetUser(context.Guild.Id, context.User.Id, out var user)
            && _cache.Pets.TryGetUsersPets(context.User.Id, out var allPets))
        {
            string[] names = input.ToLower().Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (names.Length != allPets.Count)
            {
                await context.RespondAsync(EmbedGenerator.Error($"You entered {names.Length} pet names, but you have {allPets.Count} pets"));
                return;
            }

            // Check for duplicates.
            var existingNames = new HashSet<string>();
            foreach (string name in names)
            {
                if (!existingNames.Contains(name))
                {
                    existingNames.Add(name);
                }
                else
                {
                    await context.RespondAsync(EmbedGenerator.Error($"You specified more than one pet with the name **{name}**, unable to determine the intended order."));
                    return;
                }
            }

            for (int newPos = 0; newPos < allPets.Count; newPos++)
            {
                var foundPet = allPets.Find(x => x.GetName().Equals(names[newPos], StringComparison.OrdinalIgnoreCase));
                if (foundPet != null)
                {
                    foundPet.Priority = newPos;
                }
                else
                {
                    await context.RespondAsync(EmbedGenerator.Error($"You don't have a pet with the name **{names[newPos]}**."));
                    return;
                }
            }

            await _cache.Pets.UpdatePets(allPets);

            await context.RespondAsync(EmbedGenerator.Success("Your pets have been re-ordered to the order specified."));
        }
    }

    private void SendPetLevelledUpMessage(DiscordGuild discordGuild, StringBuilder changes, ulong userId, bool pingOwner)
    {
        if (_cache.Guilds.TryGetGuild(discordGuild.Id, out var guild))
        {
            PetShared.SendPetLevelledUpMessage(changes, guild, discordGuild, userId, pingOwner).FireAndForget(_errorHandlingService);
        }
    }
}