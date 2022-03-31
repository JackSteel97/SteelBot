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

        public PetsDataHelper(DataCache cache,
            PetBefriendingService petBefriendingService,
            PetManagementService petManagementService,
            PetTreatingService petTreatingService,
            ErrorHandlingService errorHandlingService,
            ILogger<PetsDataHelper> logger)
        {
            Cache = cache;
            BefriendingService = petBefriendingService;
            ManagementService = petManagementService;
            TreatingService = petTreatingService;
            ErrorHandlingService = errorHandlingService;
            Logger = logger;
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
            if(result != default && PetShared.TryGetPetIdFromComponentId(result, out long petId))
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

                    await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder(PetMessages.GetNamingSuccessMessage(pet)));
                    args.Handled = true;
                    return;
                }
            }

            await args.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        }

        public async Task SendOwnedPetsDisplay(CommandContext context, DiscordMember target)
        {
            if (Cache.Users.TryGetUser(target.Guild.Id, target.Id, out var user)
                && Cache.Pets.TryGetUsersPets(target.Id, out var pets))
            {
                var availablePets = PetShared.GetAvailablePets(user, pets, out var disabledPets);

                List<PetWithActivation> combinedPets = PetShared.Recombine(availablePets, disabledPets);

                var baseEmbed = PetShared.GetOwnedPetsBaseEmbed(user, availablePets, disabledPets, target.DisplayName)
                    .WithThumbnail(target.AvatarUrl);

                if (combinedPets.Count == 0) {
                    baseEmbed.WithDescription("You currently own no pets.");
                    await context.RespondAsync(baseEmbed);
                    return;
                }

                var bonusCapacity = PetShared.GetBonusValue(availablePets, BonusType.PetSlots);
                var maxCapacity = PetShared.GetPetCapacity(user, bonusCapacity);
                var pages = PaginationHelper.GenerateEmbedPages(baseEmbed, combinedPets, 10, (builder, pet, _) => PetShared.AppendPetDisplayShort(builder, pet.Pet, pet.Active, maxCapacity));
                var interactivity = context.Client.GetInteractivity();
                await interactivity.SendPaginatedMessageAsync(context.Channel, context.User, pages);
            }
            else
            {
                await context.RespondAsync(PetMessages.GetNoPetsAvailableMessage(), mention: true);
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
                    var bonusCapacity = PetShared.GetBonusValue(availablePets, BonusType.PetSlots);
                    var capacity = PetShared.GetPetCapacity(user, bonusCapacity);
                    var pages = PetDisplayHelpers.GetPetBonusesSummary(combinedPets, discordMember.DisplayName, discordMember.AvatarUrl, capacity);

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

        public async Task PetXpUpdated(List<Pet> pets, DiscordGuild sourceGuild)
        {
            var changes = new StringBuilder();
            foreach (var pet in pets)
            {
                bool levelledUp = PetShared.PetXpChanged(pet, changes);
                await Cache.Pets.UpdatePet(pet);
                if (levelledUp)
                {
                    changes.AppendLine();
                }
            }

            if (changes.Length > 0 && sourceGuild != default && pets.Count > 0)
            {
                await SendPetLevelledUpMessage(sourceGuild, changes, pets[0].OwnerDiscordId);
            }
        }

        private async Task SendPetLevelledUpMessage(DiscordGuild discordGuild, StringBuilder changes, ulong userId)
        {
            if (Cache.Guilds.TryGetGuild(discordGuild.Id, out var guild))
            {
                await PetShared.SendPetLevelledUpMessage(changes, guild, discordGuild, userId);
            }
        }
    }
}