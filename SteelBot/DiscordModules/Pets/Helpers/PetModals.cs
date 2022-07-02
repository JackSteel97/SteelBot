using DSharpPlus;
using DSharpPlus.Entities;
using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.Helpers.Constants;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Helpers;

public static class PetModals
{
    public static async Task NamePet(DiscordInteraction interaction, Pet pet)
    {
        var response = new DiscordInteractionResponseBuilder()
               .WithTitle("Befriend Success")
               .WithCustomId(InteractionIds.Modals.PetNameEntry)
               .AddComponents(Interactions.Pets.NameInput(pet.RowId, $"{pet.Rarity} {pet.Species.GetName()}"));

        await interaction.CreateResponseAsync(InteractionResponseType.Modal, response);
    }

    public static async Task MovePet(DiscordInteraction interaction, Pet pet, int numberOfOwnedPets)
    {
        var response = new DiscordInteractionResponseBuilder()
            .WithTitle($"Move {pet.GetName()}")
            .WithCustomId(InteractionIds.Modals.PetMove)
            .AddComponents(Interactions.Pets.MovePositionInput(pet.RowId, numberOfOwnedPets));

        await interaction.CreateResponseAsync(InteractionResponseType.Modal, response);
    }
}