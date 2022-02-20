﻿using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets.Helpers
{
    public static class PetMessages
    {
        public static DiscordMessageBuilder GetPetOwnedSuccessMessage(DiscordMember owner, Pet pet)
        {
            var nameInsert = !string.IsNullOrWhiteSpace(pet.Name) ? Formatter.Italic(pet.Name) : "";
            var embedBuilder = new DiscordEmbedBuilder()
               .WithColor(new DiscordColor(pet.Rarity.GetColour()))
               .WithTitle("Congrats")
               .WithDescription($"{owner.Mention} Congratulations on your new pet {nameInsert}");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }

        public static DiscordMessageBuilder GetPetRanAwayMessage(Pet pet)
        {
            var embedBuilder = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(pet.Rarity.GetColour()))
                .WithTitle("It got away!")
                .WithDescription($"The {pet.Species.GetName()} ran away before you could befriend it.{Environment.NewLine}Better luck next time!");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }

        public static DiscordMessageBuilder GetBefriendFailedMessage(Pet pet)
        {
            var embedBuilder = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(pet.Rarity.GetColour()))
                .WithTitle("Failed to befriend!")
                .WithDescription($"The {pet.Species.GetName()} ran away as soon as you moved closer.{Environment.NewLine}Better luck next time!");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }

        public static DiscordMessageBuilder GetBefriendSuccessMessage(Pet pet)
        {
            var embedBuilder = EmbedGenerator.Success("What would you like to name it?", $"You befriended the {pet.Species.GetName()}!");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }

        public static DiscordMessageBuilder GetRenameRequestMessage(Pet pet)
        {
            var embedBuilder = EmbedGenerator.Primary("What would you like your their new name to be?", $"Renaming {pet.GetName()}");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }

        public static DiscordMessageBuilder GetMakePrimarySuccessMessage(Pet pet)
        {
            var embedBuilder = EmbedGenerator.Success($"{Formatter.Bold(pet.GetName())} Is now your primary pet and will receive a larger share of XP!");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }

        public static DiscordMessageBuilder GetPriorityIncreaseSuccessMessage(Pet pet)
        {
            var embedBuilder = EmbedGenerator.Success($"{Formatter.Bold(pet.GetName())} has been moved up in the priority order of your pets.");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }

        public static DiscordMessageBuilder GetPriorityDecreaseSuccessMessage(Pet pet)
        {
            var embedBuilder = EmbedGenerator.Success($"{Formatter.Bold(pet.GetName())} has been moved down in the priority order of your pets.");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }

        public static DiscordMessageBuilder GetAbandonSuccessMessage(Pet pet)
        {
            var embedBuilder = EmbedGenerator.Success($"{Formatter.Bold(pet.GetName())} has been released into the wild, freeing up a pet slot.");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }

        public static DiscordMessageBuilder GetNamingTimedOutMessage(Pet pet)
        {
            var embedBuilder = EmbedGenerator.Info($"You can give your pet {pet.Species.GetName()} a name later instead.", "Looks like you're busy");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }

        public static DiscordMessageBuilder GetPetTreatedMessage(Pet pet, int xpGain)
        {
            var embedBuilder = EmbedGenerator.Info($"{Formatter.Bold(pet.GetName())} Greatly enjoyed their treat and gained {xpGain} XP", "Tasty!");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }

        public static DiscordMessageBuilder GetNoPetsAvailableMessage()
        {
            var embedBuilder = EmbedGenerator.Info("You don't currently have any pets, use the `Pet Search` command to get some!", "No pets!");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }
    }
}