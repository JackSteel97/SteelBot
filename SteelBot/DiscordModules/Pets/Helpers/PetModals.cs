using DSharpPlus;
using DSharpPlus.Entities;
using SteelBot.Database.Models.Pets;
using SteelBot.Helpers.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Helpers
{
    public static class PetModals
    {
        public static async Task NamePet(DiscordInteraction interaction, Pet pet)
        {
            var response = new DiscordInteractionResponseBuilder()
                   .WithTitle("Befriend Success")
                   .WithCustomId(InteractionIds.Modals.PetNameEntry)
                   .AddComponents(Interactions.Pets.NameInput(pet.RowId));

            await interaction.CreateResponseAsync(InteractionResponseType.Modal, response);
        }
    }
}
