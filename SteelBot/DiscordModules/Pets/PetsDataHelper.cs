using SteelBot.DataProviders;
using System.Collections.Generic;
using System.Threading.Tasks;
using SteelBot.Database.Models.Pets;
using DSharpPlus.CommandsNext;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.DiscordModules.Pets.Services;
using SteelBot.Helpers.Extensions;
using DSharpPlus.Entities;
using SteelBot.Helpers;
using DSharpPlus;
using System;
using SteelBot.Services;
using SteelBot.DiscordModules.Pets.Models;
using DSharpPlus.Interactivity.Extensions;
using SteelBot.DiscordModules.Pets.Enums;
using System.Text;
using DSharpPlus.EventArgs;
using SteelBot.Helpers.Constants;
using System.Linq;
using Microsoft.Extensions.Logging;
using Sentry;

namespace SteelBot.DiscordModules.Pets
{
    public class PetsDataHelper
    {
        private readonly DataCache Cache;
        private readonly PetBefriendingService BefriendingService;
        private readonly PetManagementService ManagementService;
        private readonly PetTreatingService TreatingService;
        private readonly ErrorHandlingService ErrorHandlingService;
        private readonly ILogger<PetsDataHelper> Logger;
        private readonly IHub _sentry;

        public PetsDataHelper(DataCache cache,
            PetBefriendingService petBefriendingService,
            PetManagementService petManagementService,
            PetTreatingService petTreatingService,
            ErrorHandlingService errorHandlingService,
            ILogger<PetsDataHelper> logger,
            IHub sentry)
        {
            Cache = cache;
            BefriendingService = petBefriendingService;
            ManagementService = petManagementService;
            TreatingService = petTreatingService;
            ErrorHandlingService = errorHandlingService;
            Logger = logger;
            _sentry = sentry;
        }

        public async Task HandleSearch(CommandContext context)
        {
            try
            {
                await BefriendingService.Search(context);
            }
            catch (Exception e)
            {
                await ErrorHandlingService.Log(e, nameof(HandleSearch));
            }
        }

        public async Task HandleManage(CommandContext context)
        {
            try
            {
                await ManagementService.Manage(context);
            }
            catch (Exception e)
            {
                await ErrorHandlingService.Log(e, nameof(HandleManage));
            }
        }

        public async Task HandleTreat(CommandContext context)
        {
            try
            {
                await TreatingService.Treat(context);
            }
            catch (Exception e)
            {
                await ErrorHandlingService.Log(e, nameof(HandleTreat));
            }
        }

        public async Task HandleNamingPet(ModalSubmitEventArgs args)
        {
            var result = args.Values.Keys.FirstOrDefault();
            if (result != default && PetShared.TryGetPetIdFromComponentId(result, out long petId))
            {
                var newName = args.Values[result];
                if (!string.IsNullOrWhiteSpace(newName)
                    && Cache.Pets.TryGetPet(args.Interaction.User.Id, petId, out var pet)
                    && pet.OwnerDiscordId == args.Interaction.User.Id
                    && newName != pet.Name)
                {
                    Logger.LogInformation("User {UserId} is attempting to rename pet {PetId} from {OldName} to {NewName}", pet.OwnerDiscordId, pet.RowId, pet.Name, newName);
                    pet.Name = newName;
                    await Cache.Pets.UpdatePet(pet);

                    args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder(PetMessages.GetNamingSuccessMessage(pet))).FireAndForget(ErrorHandlingService);
                    args.Handled = true;
                    return;
                }
            }

            args.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate).FireAndForget(ErrorHandlingService); ;
        }

        public async Task HandleMovingPet(ModalSubmitEventArgs args)
        {
            var result = args.Values.Keys.FirstOrDefault();
            if (result != default && PetShared.TryGetPetIdFromComponentId(result, out long petId))
            {
                var newPositionText = args.Values[result];

                if (!string.IsNullOrWhiteSpace(newPositionText)
                    && int.TryParse(newPositionText, out var newPosition)
                    && Cache.Pets.TryGetPet(args.Interaction.User.Id, petId, out var petBeingMoved)
                    && newPosition - 1 != petBeingMoved.Priority)
                {
                    var newPriority = newPosition - 1;
                    Logger.LogInformation("User {UserId} is attempting to move Pet {PetId} from position {CurrentPriority} to {NewPriority}", petBeingMoved.OwnerDiscordId, petBeingMoved.RowId, petBeingMoved.Priority, newPriority);

                    await ManagementService.MovePetToPosition(petBeingMoved, newPriority);
                }
            }

            args.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate).FireAndForget(ErrorHandlingService);
        }

        public async Task SendOwnedPetsDisplay(CommandContext context, DiscordMember target)
        {
            var transaction = _sentry.GetSpan();
            if (Cache.Users.TryGetUser(target.Guild.Id, target.Id, out var user)
                && Cache.Pets.TryGetUsersPets(target.Id, out var pets))
            {
                var getPetsSpan = transaction.StartChild("Get Available Pets");
                var availablePets = PetShared.GetAvailablePets(user, pets, out var disabledPets);
                var combinedPets = PetShared.Recombine(availablePets, disabledPets);
                getPetsSpan.Finish();

                var messageBuilderSpan = transaction.StartChild("Build Message");
                var baseEmbed = PetShared.GetOwnedPetsBaseEmbed(user, pets, disabledPets.Count>0, target.DisplayName)
                    .WithThumbnail(target.AvatarUrl);

                if (combinedPets.Count == 0)
                {
                    baseEmbed.WithDescription("You currently own no pets.");
                    await context.RespondAsync(baseEmbed);
                    return;
                }

                var maxCapacity = PetShared.GetPetCapacity(user, pets);
                var baseCapacity = PetShared.GetBasePetCapacity(user);
                var pages = PaginationHelper.GenerateEmbedPages(baseEmbed, combinedPets, 10, (builder, pet, _) => PetShared.AppendPetDisplayShort(builder, pet.Pet, pet.Active, baseCapacity, maxCapacity));
                var interactivity = context.Client.GetInteractivity();
                messageBuilderSpan.Finish();

                var displaySpan = transaction.StartChild("Display Paginated Message");
                await interactivity.SendPaginatedMessageAsync(context.Channel, context.User, pages);
                displaySpan.Finish();
            }
            else
            {
                context.RespondAsync(PetMessages.GetNoPetsAvailableMessage(), mention: true).FireAndForget(ErrorHandlingService);
            }
        }

        public Task SendPetBonusesDisplay(CommandContext context, DiscordMember discordMember)
        {
            if (Cache.Users.TryGetUser(discordMember.Guild.Id, discordMember.Id, out var user)
                && Cache.Pets.TryGetUsersPets(discordMember.Id, out var pets))
            {
                var availablePets = PetShared.GetAvailablePets(user, pets, out var disabledPets);
                var combinedPets = PetShared.Recombine(availablePets, disabledPets);
                if (combinedPets.Count > 0)
                {
                    var maxCapacity = PetShared.GetPetCapacity(user, pets);
                    var baseCapacity = PetShared.GetBasePetCapacity(user);
                    var pages = PetDisplayHelpers.GetPetBonusesSummary(combinedPets, discordMember.DisplayName, discordMember.AvatarUrl, baseCapacity, maxCapacity);

                    var interactivity = context.Client.GetInteractivity();
                    return interactivity.SendPaginatedMessageAsync(context.Channel, context.User, pages);
                }
            }
            return context.RespondAsync(PetMessages.GetNoPetsAvailableMessage(), mention: true);
        }

        public List<Pet> GetAvailablePets(ulong guildId, ulong userId, out List<Pet> disabledPets)
        {
            if (Cache.Users.TryGetUser(guildId, userId, out var user) && Cache.Pets.TryGetUsersPets(userId, out var pets))
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
                bool levelledUp = PetShared.PetXpChanged(pet, changes, levelOfUser, out var shouldPingOwner);
                if (levelledUp)
                {
                    if (shouldPingOwner)
                    {
                        pingOwner = true;
                    }
                    changes.AppendLine();
                }
            }

            await Cache.Pets.UpdatePets(pets);

            if (changes.Length > 0 && sourceGuild != default && pets.Count > 0)
            {
                SendPetLevelledUpMessage(sourceGuild, changes, pets[0].OwnerDiscordId, pingOwner);
            }
        }

        public async Task Reorder(CommandContext context, string input)
        {
            if (Cache.Users.TryGetUser(context.Guild.Id, context.User.Id, out var user)
                && Cache.Pets.TryGetUsersPets(context.User.Id, out var allPets))
            {
                var names = input.ToLower().Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if(names.Length != allPets.Count)
                {
                    await context.RespondAsync(EmbedGenerator.Error($"You entered {names.Length} pet names, but you have {allPets.Count} pets"));
                    return;
                }

                // Check for duplicates.
                HashSet<string> existingNames = new HashSet<string>();
                foreach(var name in names)
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

                for(int newPos = 0; newPos < allPets.Count; newPos++)
                {
                    var foundPet = allPets.Find(x=>x.GetName().Equals(names[newPos], StringComparison.OrdinalIgnoreCase));
                    if(foundPet != null)
                    {
                        foundPet.Priority = newPos;
                    }
                    else
                    {
                        await context.RespondAsync(EmbedGenerator.Error($"You don't have a pet with the name **{names[newPos]}**."));
                        return;
                    }
                }

                await Cache.Pets.UpdatePets(allPets);

                await context.RespondAsync(EmbedGenerator.Success("Your pets have been re-ordered to the order specified."));
            }
        }

        private void SendPetLevelledUpMessage(DiscordGuild discordGuild, StringBuilder changes, ulong userId, bool pingOwner)
        {
            if (Cache.Guilds.TryGetGuild(discordGuild.Id, out var guild))
            {
                PetShared.SendPetLevelledUpMessage(changes, guild, discordGuild, userId, pingOwner).FireAndForget(ErrorHandlingService);
            }
        }
    }
}