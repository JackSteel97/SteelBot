using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Sentry;
using SteelBot.DataProviders;
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

namespace SteelBot.DiscordModules.Pets.Services
{
    public class PetTreatingService
    {
        private readonly DataCache Cache;
        private readonly ErrorHandlingService ErrorHandlingService;
        private readonly IHub _sentry;

        public PetTreatingService(DataCache cache, ErrorHandlingService errorHandlingService, IHub sentry)
        {
            Cache = cache;
            ErrorHandlingService = errorHandlingService;
            _sentry = sentry;
        }

        public async Task Treat(CommandContext context)
        {
            var transaction = _sentry.GetSpan();
            if (Cache.Users.TryGetUser(context.Guild.Id, context.User.Id, out var user)
                && Cache.Pets.TryGetUsersPets(context.User.Id, out var allPets))
            {
                var getPetsSpan = transaction.StartChild("Get Available Pets");
                var availablePets = PetShared.GetAvailablePets(user, allPets, out var disabledPets);
                var combinedPets = PetShared.Recombine(availablePets, disabledPets);
                getPetsSpan.Finish();

                var buildEmbedSpan = transaction.StartChild("Build Owned Pets Base Embed");
                var baseEmbed = PetShared.GetOwnedPetsBaseEmbed(user, availablePets, disabledPets.Count > 0);
                buildEmbedSpan.Finish();

                var capacitySpan = transaction.StartChild("Get Pet Capacity");
                var maxCapacity = PetShared.GetPetCapacity(user, allPets);
                var baseCapacity = PetShared.GetBasePetCapacity(user);
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
                        await HandleTreatGiven(context, petId);
                    }
                    handleSpan.Finish();
                }
            }
            else
            {
                context.RespondAsync(PetMessages.GetNoPetsAvailableMessage(), mention: true).FireAndForget(ErrorHandlingService);
            }
        }

        private async Task HandleTreatGiven(CommandContext context, long petId)
        {
            var transaction = _sentry.GetSpan();
            if (Cache.Pets.TryGetPet(context.Member.Id, petId, out var pet) && Cache.Users.TryGetUser(context.Guild.Id, context.Member.Id, out var user))
            {
                var xpMathsSpan = transaction.StartChild("Calculate Treat Xp");
                var xpRequiredForNextLevel = LevellingMaths.PetXpForLevel(pet.CurrentLevel + 1, pet.Rarity);
                var xpRequiredForThisLevel = LevellingMaths.PetXpForLevel(pet.CurrentLevel, pet.Rarity);
                var xpRequiredToLevel = xpRequiredForNextLevel - xpRequiredForThisLevel;
                var upperBound = Math.Max(101, (int)Math.Round(xpRequiredToLevel));
                var xpGain = RandomNumberGenerator.GetInt32(100, upperBound);
                pet.EarnedXp += xpGain;
                xpMathsSpan.Finish();

                var xpChangedSpan = transaction.StartChild("Pet Xp Changed");
                var changes = new StringBuilder();
                bool levelledUp = PetShared.PetXpChanged(pet, changes, user.CurrentLevel, out var shouldPingOwner);
                xpChangedSpan.Finish();
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
