﻿using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using SteelBot.Database.Models.Pets;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.Helpers;
using SteelBot.Helpers.Constants;
using SteelBot.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Services
{
    public class PetManagementService
    {
        private readonly DataCache Cache;

        public PetManagementService(DataCache cache)
        {
            Cache = cache;
        }

        public async Task Manage(CommandContext context)
        {
            if (Cache.Users.TryGetUser(context.Guild.Id, context.User.Id, out var user)
                && Cache.Pets.TryGetUsersPets(context.User.Id, out var allPets))
            {
                var petList = PetShared.GetOwnedPetsDisplayEmbed(user, allPets);

                var initialResponseBuilder = new DiscordMessageBuilder().WithEmbed(petList);

                var components = allPets.OrderBy(p => p.Priority).Select(p => Interactions.Pets.Manage(p.RowId, p.GetName())).ToList();
                components.Add(Interactions.Confirmation.Cancel);
                initialResponseBuilder = InteractivityHelper.AddComponents(initialResponseBuilder, components);

                var message = await context.RespondAsync(initialResponseBuilder, mention: true);
                var result = await message.WaitForButtonAsync(context.Member);

                initialResponseBuilder.ClearComponents();

                if (!result.TimedOut && result.Result.Id != InteractionIds.Confirmation.Cancel)
                {
                    await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(initialResponseBuilder));

                    // Figure out which pet they want to manage.
                    if (PetShared.TryGetPetIdFromPetSelectorButton(result.Result.Id, out var petId))
                    {
                        await HandleManagePet(context, petId);
                    }
                }
                else
                {
                    await message.ModifyAsync(initialResponseBuilder);
                }
            }
            else
            {
                await context.RespondAsync(PetMessages.GetNoPetsAvailableMessage(), mention: true);
            }
        }

        private async Task HandleManagePet(CommandContext context, long petId)
        {
            if (Cache.Pets.TryGetPet(context.Member.Id, petId, out var pet))
            {
                Cache.Pets.TryGetUsersPetsCount(context.Member.Id, out var ownedPetCount);
                var petDisplay = PetDisplayHelpers.GetPetDisplayEmbed(pet);
                var initialResponseBuilder = new DiscordMessageBuilder()
                    .WithEmbed(petDisplay);

                initialResponseBuilder = InteractivityHelper.AddComponents(initialResponseBuilder, new DiscordComponent[]
                    {
                        Interactions.Pets.Rename,
                        Interactions.Pets.MakePrimary.Disable(pet.IsPrimary),
                        Interactions.Pets.IncreasePriority.Disable(pet.IsPrimary),
                        Interactions.Pets.DecreasePriority.Disable(pet.Priority == (ownedPetCount-1)),
                        Interactions.Pets.Abandon,
                        Interactions.Confirmation.Cancel,
                    });

                var message = await context.RespondAsync(initialResponseBuilder, mention: true);
                var result = await message.WaitForButtonAsync(context.Member);

                initialResponseBuilder.ClearComponents();

                if (!result.TimedOut && result.Result.Id != InteractionIds.Confirmation.Cancel)
                {
                    await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(initialResponseBuilder));
                    switch (result.Result.Id)
                    {
                        case InteractionIds.Pets.Rename:
                            await HandleRenamingPet(context, pet);
                            break;
                        case InteractionIds.Pets.MakePrimary:
                            await HandleMakePrimary(context, pet);
                            break;
                        case InteractionIds.Pets.IncreasePriority:
                            await HandlePriorityIncrease(context, pet);
                            break;
                        case InteractionIds.Pets.DecreasePriority:
                            await HandlePriorityDecrease(context, pet);
                            break;
                        case InteractionIds.Pets.Abandon:
                            await HandlePetAbandon(context, pet);
                            break;
                    }
                }
                else
                {
                    await message.ModifyAsync(initialResponseBuilder);
                }
            }
            else
            {
                await context.RespondAsync(EmbedGenerator.Error("Something went wrong and I couldn't find that pet. Please try again later."), mention: true);
            }
        }

        private async Task HandleRenamingPet(CommandContext context, Pet pet)
        {
            var message = PetMessages.GetRenameRequestMessage(pet);
            await context.Channel.SendMessageAsync(message);
            (bool nameChanged, ulong nameMessageId) = await PetShared.HandleNamingPet(context, pet);
            if (nameChanged)
            {
                await Cache.Pets.UpdatePet(pet);
                var successMessage = new DiscordMessageBuilder().WithEmbed(EmbedGenerator.Success($"Renamed to {Formatter.Italic(pet.Name)}")).WithReply(nameMessageId, true);
                await context.Channel.SendMessageAsync(successMessage);
            }
        }

        private async Task HandleMakePrimary(CommandContext context, Pet pet)
        {
            int oldPriority = pet.Priority;
            if (Cache.Pets.TryGetUsersPets(context.Member.Id, out var allPets))
            {
                foreach (var ownedPet in allPets)
                {
                    if (ownedPet.Priority < oldPriority)
                    {
                        ++ownedPet.Priority;
                        await Cache.Pets.UpdatePet(ownedPet);
                    }
                }
                pet.Priority = 0;
                await Cache.Pets.UpdatePet(pet);
                await context.Channel.SendMessageAsync(PetMessages.GetMakePrimarySuccessMessage(pet));
            }
        }

        private async Task HandlePriorityIncrease(CommandContext context, Pet pet)
        {
            int oldPriority = pet.Priority;
            if (Cache.Pets.TryGetUsersPets(context.Member.Id, out var allPets))
            {
                foreach (var ownedPet in allPets)
                {
                    if (ownedPet.Priority == oldPriority - 1)
                    {
                        ++ownedPet.Priority;
                        await Cache.Pets.UpdatePet(ownedPet);
                        break;
                    }
                }
                --pet.Priority;
                await Cache.Pets.UpdatePet(pet);
                await context.Channel.SendMessageAsync(PetMessages.GetPriorityIncreaseSuccessMessage(pet));
            }
        }

        private async Task HandlePriorityDecrease(CommandContext context, Pet pet)
        {
            int oldPriority = pet.Priority;
            if (Cache.Pets.TryGetUsersPets(context.Member.Id, out var allPets))
            {
                foreach (var ownedPet in allPets)
                {
                    if (ownedPet.Priority == oldPriority + 1)
                    {
                        --ownedPet.Priority;
                        await Cache.Pets.UpdatePet(ownedPet);
                        break;
                    }
                }
                ++pet.Priority;
                await Cache.Pets.UpdatePet(pet);
                await context.Channel.SendMessageAsync(PetMessages.GetPriorityDecreaseSuccessMessage(pet));
            }
        }

        private async Task HandlePetAbandon(CommandContext context, Pet pet)
        {
            if (await InteractivityHelper.GetConfirmation(context, "Pet Release"))
            {
                await Cache.Pets.RemovePet(context.Member.Id, pet.RowId);

                if (Cache.Pets.TryGetUsersPets(context.Member.Id, out var allPets))
                {
                    foreach (var ownedPet in allPets)
                    {
                        if (ownedPet.Priority > pet.Priority)
                        {
                            --ownedPet.Priority;
                            await Cache.Pets.UpdatePet(ownedPet);
                        }
                    }
                }

                await context.Channel.SendMessageAsync(PetMessages.GetAbandonSuccessMessage(pet));
            }
        }
    }
}