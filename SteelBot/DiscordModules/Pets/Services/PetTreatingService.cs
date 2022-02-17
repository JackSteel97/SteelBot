using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.Helpers;
using SteelBot.Helpers.Constants;
using SteelBot.Helpers.Levelling;
using System;
using System.Linq;
using System.Security.Cryptography;
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
                var petList = PetShared.GetOwnedPetsDisplayEmbed(user, allPets);

                var initialResponseBuilder = new DiscordMessageBuilder().WithEmbed(petList);

                var components = allPets.OrderBy(p => p.Priority).Select(p => Interactions.Pets.Treat(p.RowId, p.GetName()));
                initialResponseBuilder = InteractivityHelper.AddComponents(initialResponseBuilder, components);

                var message = await context.RespondAsync(initialResponseBuilder);
                var result = await message.WaitForButtonAsync();

                initialResponseBuilder.ClearComponents();
                if (!result.TimedOut)
                {
                    await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(initialResponseBuilder));
                    if (PetShared.TryGetPetIdFromPetSelectorButton(result.Result.Id, out long petId))
                    {
                        await HandleTreatGiven(context, petId);
                    }
                }
                else
                {
                    await message.ModifyAsync(initialResponseBuilder);
                }
            }
            else
            {
                await context.RespondAsync(PetMessages.GetNoPetsAvailableMessage());
            }
        }

        private async Task HandleTreatGiven(CommandContext context, long petId)
        {
            if (Cache.Pets.TryGetPet(context.Member.Id, petId, out var pet))
            {
                var xpRequiredToLevel = LevellingMaths.XpForLevel(pet.CurrentLevel + 1);
                var upperBound = (int)Math.Round(xpRequiredToLevel * 0.1);
                var xpGain = RandomNumberGenerator.GetInt32(1, upperBound);
                pet.EarnedXp += xpGain;

                PetShared.PetXpChanged(pet);
                await Cache.Pets.UpdatePet(pet);
                await context.Channel.SendMessageAsync(PetMessages.GetPetTreatedMessage(pet, xpGain));
            }
        }
    }
}
