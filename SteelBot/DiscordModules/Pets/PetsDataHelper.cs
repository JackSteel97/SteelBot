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

namespace SteelBot.DiscordModules.Pets
{
    public class PetsDataHelper
    {
        private readonly DataCache Cache;
        private readonly PetBefriendingService BefriendingService;
        private readonly PetManagementService ManagementService;
        private readonly PetTreatingService TreatingService;
        private readonly ErrorHandlingService ErrorHandlingService;

        public PetsDataHelper(DataCache cache,
            PetBefriendingService petBefriendingService,
            PetManagementService petManagementService,
            PetTreatingService petTreatingService,
            ErrorHandlingService errorHandlingService)
        {
            Cache = cache;
            BefriendingService = petBefriendingService;
            ManagementService = petManagementService;
            TreatingService = petTreatingService;
            ErrorHandlingService = errorHandlingService;
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

        public Task SendOwnedPetsDisplay(CommandContext context, DiscordMember target)
        {
            if (Cache.Users.TryGetUser(target.Guild.Id, target.Id, out var user)
                && Cache.Pets.TryGetUsersPets(target.Id, out var pets))
            {
                var embed = PetShared.GetOwnedPetsDisplayEmbed(user, pets, target.DisplayName);
                embed.WithThumbnail(target.AvatarUrl);
                return context.RespondAsync(embed, mention: true);
            }
            else
            {
                return context.RespondAsync(PetMessages.GetNoPetsAvailableMessage(), mention: true);
            }
        }

        public Task SendPetBonusesDisplay(CommandContext context, DiscordMember discordMember)
        {
            if (Cache.Users.TryGetUser(discordMember.Guild.Id, discordMember.Id, out _)
                && Cache.Pets.TryGetUsersPets(discordMember.Id, out var pets))
            {
                var embed = PetDisplayHelpers.GetPetBonusesSummary(pets, discordMember.DisplayName);
                embed.WithThumbnail(discordMember.AvatarUrl);
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

        public async Task PetXpUpdated(List<Pet> pets, DiscordGuild sourceGuild)
        {
            foreach (var pet in pets)
            {
                bool levelledUp = PetShared.PetXpChanged(pet);
                await Cache.Pets.UpdatePet(pet);
                if (levelledUp && sourceGuild != default)
                {
                    await SendPetLevelledUpMessage(pet, sourceGuild);
                }
            }
        }

        private async Task SendPetLevelledUpMessage(Pet pet, DiscordGuild discordGuild)
        {
            if (Cache.Guilds.TryGetGuild(discordGuild.Id, out var guild))
            {
                await PetShared.SendPetLevelledUpMessage(pet, guild, discordGuild);
            }
        }
    }
}