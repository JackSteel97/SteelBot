using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using SteelBot.Database.Models.Pets;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.DiscordModules.Pets.Services;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets;

public class PetsDataHelper
{
    private readonly DataCache _cache;
    private readonly PetManagementService _managementService;
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly ILogger<PetsDataHelper> _logger;

    public PetsDataHelper(DataCache cache,
        PetManagementService petManagementService,
        ErrorHandlingService errorHandlingService,
        ILogger<PetsDataHelper> logger)
    {
        _cache = cache;
        _managementService = petManagementService;
        _errorHandlingService = errorHandlingService;
        _logger = logger;
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
                args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(EmbedGenerator.Success($"{petBeingMoved.Name} moved to position {newPosition}"))).FireAndForget(_errorHandlingService);
                return;
            }
        }
        args.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate).FireAndForget(_errorHandlingService);
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
    
    private void SendPetLevelledUpMessage(DiscordGuild discordGuild, StringBuilder changes, ulong userId, bool pingOwner)
    {
        if (_cache.Guilds.TryGetGuild(discordGuild.Id, out var guild))
        {
            PetShared.SendPetLevelledUpMessage(changes, guild, discordGuild, userId, pingOwner).FireAndForget(_errorHandlingService);
        }
    }
}