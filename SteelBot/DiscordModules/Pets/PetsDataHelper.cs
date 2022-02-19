using SteelBot.DataProviders;
using System.Collections.Generic;
using System.Threading.Tasks;
using SteelBot.Database.Models.Pets;
using DSharpPlus.CommandsNext;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.DiscordModules.Pets.Services;
using SteelBot.Helpers.Extensions;

namespace SteelBot.DiscordModules.Pets
{
    public class PetsDataHelper
    {
        private readonly DataCache Cache;
        private readonly PetBefriendingService BefriendingService;
        private readonly PetManagementService ManagementService;
        private readonly PetTreatingService TreatingService;

        public PetsDataHelper(DataCache cache,
            PetBefriendingService petBefriendingService,
            PetManagementService petManagementService,
            PetTreatingService petTreatingService)
        {
            Cache = cache;
            BefriendingService = petBefriendingService;
            ManagementService = petManagementService;
            TreatingService = petTreatingService;
        }

        public Task HandleSearch(CommandContext context)
        {
            return BefriendingService.Search(context);
        }

        public Task HandleManage(CommandContext context)
        {
            return ManagementService.Manage(context);
        }

        public Task HandleTreat(CommandContext context)
        {
            return TreatingService.Treat(context);
        }

        public Task SendOwnedPetsDisplay(CommandContext context)
        {
            if (Cache.Users.TryGetUser(context.Guild.Id, context.User.Id, out var user)
                && Cache.Pets.TryGetUsersPets(context.User.Id, out var pets))
            {
                var embed = PetShared.GetOwnedPetsDisplayEmbed(user, pets);
                embed.WithThumbnail(context.User.AvatarUrl);
                return context.RespondAsync(embed, mention: true);
            }
            else
            {
                return context.RespondAsync(PetMessages.GetNoPetsAvailableMessage(), mention: true);
            }
        }

        public Task SendPetBonusesDisplay(CommandContext context)
        {
            if (Cache.Users.TryGetUser(context.Guild.Id, context.User.Id, out var user)
                && Cache.Pets.TryGetUsersPets(context.User.Id, out var pets))
            {
                var embed = PetDisplayHelpers.GetPetBonusesSummary(pets);
                embed.WithThumbnail(context.User.AvatarUrl);
                return context.RespondAsync(embed, mention: true);
            }
            else
            {
                return context.RespondAsync(PetMessages.GetNoPetsAvailableMessage(), mention: true);
            }
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

        public async Task PetXpUpdated(List<Pet> pets)
        {
            foreach (var pet in pets)
            {
                PetShared.PetXpChanged(pet);
                await Cache.Pets.UpdatePet(pet);
            }
        }
    }
}