using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.Helpers;
using SteelBot.Helpers.Constants;
using SteelBot.Helpers.Extensions;
using SteelBot.Helpers.Levelling;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Services
{
    public class PetTreatingService
    {
        private readonly DataCache Cache;

        public PetTreatingService(DataCache cache)
        {
            Cache = cache;
        }

        public async Task Treat(CommandContext context)
        {
            if (Cache.Users.TryGetUser(context.Guild.Id, context.User.Id, out var user)
                && Cache.Pets.TryGetUsersPets(context.User.Id, out var allPets))
            {
                var availablePets = PetShared.GetAvailablePets(user, allPets, out var disabledPets);
                var combinedPets = PetShared.Recombine(availablePets, disabledPets);

                var baseEmbed = PetShared.GetOwnedPetsBaseEmbed(user, availablePets, disabledPets);

                if (combinedPets.Count == 0)
                {
                    baseEmbed.WithDescription("You currently own no pets.");
                    await context.RespondAsync(baseEmbed);
                    return;
                }

                var bonusCapacity = PetShared.GetBonusValue(availablePets, Enums.BonusType.PetSlots);
                var maxCapacity = PetShared.GetPetCapacity(user, bonusCapacity);
                var pages = PaginationHelper.GenerateEmbedPages(baseEmbed, combinedPets, 10,
                    (builder, pet) => PetShared.AppendPetDisplayShort(builder, pet.Pet, pet.Active, maxCapacity),
                    (pet) => Interactions.Pets.Treat(pet.Pet.RowId, pet.Pet.GetName()));

                var resultId = await InteractivityHelper.SendPaginatedMessageWithComponentsAsync(context.Channel, context.User, pages);
                if (!string.IsNullOrWhiteSpace(resultId))
                {
                    // Figure out which pet they want to manage.
                    if (PetShared.TryGetPetIdFromComponentId(resultId, out long petId))
                    {
                        await HandleTreatGiven(context, petId);
                    }
                }
            }
            else
            {
                await context.RespondAsync(PetMessages.GetNoPetsAvailableMessage(), mention: true);
            }
        }

        private async Task HandleTreatGiven(CommandContext context, long petId)
        {
            if (Cache.Pets.TryGetPet(context.Member.Id, petId, out var pet))
            {
                var xpRequiredForNextLevel = LevellingMaths.PetXpForLevel(pet.CurrentLevel + 1, pet.Rarity);
                var xpRequiredToLevel = xpRequiredForNextLevel - pet.EarnedXp;
                var upperBound = Math.Max(101, (int)Math.Round(xpRequiredToLevel * 0.1));
                var xpGain = RandomNumberGenerator.GetInt32(100, upperBound);
                pet.EarnedXp += xpGain;

                var changes = new StringBuilder();
                bool levelledUp = PetShared.PetXpChanged(pet, changes);
                await Cache.Pets.UpdatePet(pet);
                await context.Channel.SendMessageAsync(PetMessages.GetPetTreatedMessage(pet, xpGain));
                if (levelledUp && Cache.Guilds.TryGetGuild(context.Guild.Id, out var guild))
                {
                    await PetShared.SendPetLevelledUpMessage(changes, guild, context.Guild, context.Member.Id);
                }
            }
        }
    }
}
