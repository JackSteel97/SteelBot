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
using SteelBot.Services;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Services
{
    public class PetTreatingService
    {
        private readonly DataCache Cache;
        private readonly ErrorHandlingService ErrorHandlingService;

        public PetTreatingService(DataCache cache, ErrorHandlingService errorHandlingService)
        {
            Cache = cache;
            ErrorHandlingService = errorHandlingService;
        }

        public async Task Treat(CommandContext context)
        {
            if (Cache.Users.TryGetUser(context.Guild.Id, context.User.Id, out var user)
                && Cache.Pets.TryGetUsersPets(context.User.Id, out var allPets))
            {
                var availablePets = PetShared.GetAvailablePets(user, allPets, out var disabledPets);
                var combinedPets = PetShared.Recombine(availablePets, disabledPets);

                var baseEmbed = PetShared.GetOwnedPetsBaseEmbed(user, availablePets, disabledPets.Count > 0);

                var maxCapacity = PetShared.GetPetCapacity(user, allPets);
                var baseCapacity = PetShared.GetBasePetCapacity(user);
                var pages = PaginationHelper.GenerateEmbedPages(baseEmbed, combinedPets, 10,
                    (builder, pet) => PetShared.AppendPetDisplayShort(builder, pet.Pet, pet.Active, baseCapacity, maxCapacity),
                    (pet) => Interactions.Pets.Treat(pet.Pet.RowId, pet.Pet.GetName()));

                (string resultId, _) = await InteractivityHelper.SendPaginatedMessageWithComponentsAsync(context.Channel, context.User, pages);
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
                context.RespondAsync(PetMessages.GetNoPetsAvailableMessage(), mention: true).FireAndForget(ErrorHandlingService);
            }
        }

        private async Task HandleTreatGiven(CommandContext context, long petId)
        {
            if (Cache.Pets.TryGetPet(context.Member.Id, petId, out var pet) && Cache.Users.TryGetUser(context.Guild.Id, context.Member.Id, out var user))
            {
                var xpRequiredForNextLevel = LevellingMaths.PetXpForLevel(pet.CurrentLevel + 1, pet.Rarity);
                var xpRequiredForThisLevel = LevellingMaths.PetXpForLevel(pet.CurrentLevel, pet.Rarity);
                var xpRequiredToLevel = xpRequiredForNextLevel - xpRequiredForThisLevel;
                var upperBound = Math.Max(101, (int)Math.Round(xpRequiredToLevel * 0.1));
                var xpGain = RandomNumberGenerator.GetInt32(100, upperBound);
                pet.EarnedXp += xpGain;

                var changes = new StringBuilder();
                bool levelledUp = PetShared.PetXpChanged(pet, changes, user.CurrentLevel, out var shouldPingOwner);
                await Cache.Pets.UpdatePet(pet);
                context.Channel.SendMessageAsync(PetMessages.GetPetTreatedMessage(pet, xpGain)).FireAndForget(ErrorHandlingService);
                if (levelledUp && Cache.Guilds.TryGetGuild(context.Guild.Id, out var guild))
                {
                    PetShared.SendPetLevelledUpMessage(changes, guild, context.Guild, context.Member.Id, shouldPingOwner).FireAndForget(ErrorHandlingService);
                }
            }
        }
    }
}
