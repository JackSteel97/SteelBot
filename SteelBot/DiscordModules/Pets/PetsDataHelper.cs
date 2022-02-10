using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using SteelBot.DataProviders;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using DSharpPlus;
using SteelBot.Helpers.Extensions;
using Humanizer;
using SteelBot.Helpers;
using SteelBot.DiscordModules.Pets.Generation;
using DSharpPlus.CommandsNext;
using SteelBot.Helpers.Constants;
using DSharpPlus.Interactivity.Extensions;
using System.Security.Cryptography;

namespace SteelBot.DiscordModules.Pets
{
    public class PetsDataHelper
    {
        private readonly DataCache Cache;
        private readonly AppConfigurationService AppConfigurationService;
        private readonly PetFactory PetFactory;
        private readonly ILogger<PetsDataHelper> Logger;

        public PetsDataHelper(DataCache cache, AppConfigurationService appConfigurationService, PetFactory petFactory, ILogger<PetsDataHelper> logger)
        {
            Cache = cache;
            AppConfigurationService = appConfigurationService;
            PetFactory = petFactory;
            Logger = logger;
        }
        public DiscordMessageBuilder GetPetOwnedSuccessMessage(DiscordMember owner, Pet pet)
        {
            var nameInsert = !string.IsNullOrWhiteSpace(pet.Name) ? Formatter.Italic(pet.Name) : "";
            var embedBuilder = new DiscordEmbedBuilder()
               .WithColor(new DiscordColor(pet.Rarity.GetColour()))
               .WithTitle("Congrats")
               .WithDescription($"{owner.Mention} Congratulations on your new pet {nameInsert}");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }

        public DiscordMessageBuilder GetPetRanAwayMessage(Pet pet)
        {
            var embedBuilder = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(pet.Rarity.GetColour()))
                .WithTitle("It got away!")
                .WithDescription($"The {pet.Species.GetName()} ran away before you could befriend it.{Environment.NewLine}Better luck next time!");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }

        public DiscordMessageBuilder GetBefriendFailedMessage(Pet pet)
        {
            var embedBuilder = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(pet.Rarity.GetColour()))
                .WithTitle("Failed to befriend!")
                .WithDescription($"The {pet.Species.GetName()} ran away as soon as you moved closer.{Environment.NewLine}Better luck next time!");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }

        public DiscordMessageBuilder GetBefriendSuccessMessage(Pet pet)
        {
            var embedBuilder = EmbedGenerator.Success("What would you like to name it?", $"You befriended the {pet.Species.GetName()}!");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }

        public DiscordMessageBuilder GetNamingTimedOutMessage(Pet pet)
        {
            var embedBuilder = EmbedGenerator.Info($"You can give your pet {pet.Species.GetName()} a name later instead.", $"Looks like you're busy");
            return new DiscordMessageBuilder().WithEmbed(embedBuilder);
        }

        public DiscordEmbedBuilder GetPetDisplayEmbed(Pet pet, bool includeName = true)
        {
            string name;
            if (includeName)
            {
                name = $"{pet.Name ?? $"Unamed {pet.Species.GetName()}"} - ";
            }
            else
            {
                name = $"{pet.Species.GetName()} - ";
            }
            var embedBuilder = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(pet.Rarity.GetColour()))
                .WithTitle($"{name}Level {pet.CurrentLevel}")
                .AddField("Rarity", Formatter.InlineCode(pet.Rarity.ToString()), true)
                .AddField("Species", Formatter.InlineCode(pet.Species.GetName()), true)
                .AddField("Size", Formatter.InlineCode(pet.Size.ToString()), true)
                .AddField("Age", Formatter.InlineCode($"{GetAge(pet.BornAt)}"), true)
                .AddField("Found", Formatter.InlineCode(pet.FoundAt.Humanize()), true);

            foreach (var attribute in pet.Attributes)
            {
                embedBuilder.AddField(attribute.Name, Formatter.InlineCode(attribute.Description), true);
            }

            StringBuilder bonuses = new StringBuilder();
            foreach (var bonus in pet.Bonuses)
            {
                bonuses.Append('`').Append(bonus.BonusType.Humanize()).Append(" XP").Append(": ");
                if (bonus.BonusType.IsNegative())
                {
                    bonuses.Append('-');
                }
                else
                {
                    bonuses.Append('+');
                }
                bonuses.Append(bonus.PercentageValue.ToString("P2")).AppendLine("`");
            }

            embedBuilder.AddField("Bonuses", bonuses.ToString());

            return embedBuilder;
        }

        public bool SearchSuccess(DiscordMember userSearching)
        {
            var probability = GetSearchSuccessProbability(userSearching);
            return MathsHelper.TrueWithProbability(probability);
        }

        public bool BefriendSuccess(DiscordMember user, Pet target)
        {
            var probability = GetBefriendSuccessProbability(user, target);
            return MathsHelper.TrueWithProbability(probability);
        }

        public async Task<(bool befriendAttempt, Pet pet)> HandleInitialSearchSuccess(CommandContext context)
        {
            bool befriendAttempt = false;
            var hasSpace = HasSpaceForAnotherPet(context.Member);
            string noSpaceMessage = "";
            if (!hasSpace)
            {
                noSpaceMessage = " But you don't have enough room for another pet!";
            }

            var foundPet = PetFactory.Generate();
            var initialPetDisplay = GetPetDisplayEmbed(foundPet, includeName: false);

            var initialResponseBuilder = new DiscordMessageBuilder()
                .WithContent($"You found a new potential friend!{noSpaceMessage}")
                .WithEmbed(initialPetDisplay)
                .AddComponents(new DiscordComponent[] {
                    Interactions.Pets.Befriend.Disable(!hasSpace),
                    Interactions.Pets.Leave
                });

            var message = await context.RespondAsync(initialResponseBuilder);
            var result = await message.WaitForButtonAsync(context.Member);

            if (!result.TimedOut)
            {
                initialResponseBuilder.ClearComponents();
                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(initialResponseBuilder));
                befriendAttempt = result.Result.Id == InteractionIds.Pets.Befriend;
            }
            else
            {
                await message.DeleteAsync();
                await context.RespondAsync(GetPetRanAwayMessage(foundPet));
            }

            return (befriendAttempt, foundPet);
        }

        public async Task<bool> HandleBefriendAttempt(CommandContext context, Pet pet)
        {
            bool befriendSuccess = BefriendSuccess(context.Member, pet);
            if (befriendSuccess)
            {
                await HandleBefriendSuccess(context, pet);
            }
            else
            {
                var response = GetBefriendFailedMessage(pet).WithReply(context.Message.Id, mention: true);
                await context.Channel.SendMessageAsync(response);
            }
            return befriendSuccess;
        }

        public async Task HandleBefriendSuccess(CommandContext context, Pet pet)
        {
            var successMessage = GetBefriendSuccessMessage(pet).WithReply(context.Message.Id, mention: true);
            await context.Channel.SendMessageAsync(successMessage);
            await HandleNamingPet(context, pet);
        }

        public async Task HandleNamingPet(CommandContext context, Pet pet)
        {
            bool named = false;
            while (!named)
            {
                DiscordMessage nextMessage = null;
                var nameResult = await context.Message.GetNextMessageAsync(m =>
                {
                    nextMessage = m;
                    return true;
                });

                if (!nameResult.TimedOut)
                {
                    named = await ValidateAndName(pet, nextMessage);
                }
                else
                {
                    await context.Channel.SendMessageAsync(GetNamingTimedOutMessage(pet).WithReply(context.Message.Id, mention: true));
                    named = true;
                }
                if (!named)
                {
                    await nextMessage.RespondAsync(EmbedGenerator.Primary($"What would you like to name your new {pet.Species.GetName()} instead?", "Ok, try again"));
                }
            }
        }

        public List<Pet> GetAvailablePets(ulong guildId, ulong userId)
        {
            if (Cache.Pets.TryGetUsersPets(userId, out var pets))
            {
                var capacity = GetPetCapacity(guildId, userId);
                var availableCount = Math.Min(capacity, pets.Count);
                return pets.OrderBy(p => p.Priority).Take(availableCount).ToList();
            }
            else
            {
                return new List<Pet>();
            }
        }

        private async Task<bool> ValidateAndName(Pet pet, DiscordMessage nameMessage)
        {
            bool named = false;
            if (nameMessage.Content.Length > 255)
            {
                await nameMessage.RespondAsync(EmbedGenerator.Warning("Sorry, that name is too long. Please try something else"));
            }
            else if (await ConfirmNaming(pet, nameMessage))
            {
                pet.Name = nameMessage.Content;
                named = true;
            }

            return named;
        }

        private async Task<bool> ConfirmNaming(Pet pet, DiscordMessage nameMessage)
        {
            bool named;

            var confirmationEmbed = EmbedGenerator.Info($"Ok, you'd like to name this {pet.Species.GetName()} \"{Formatter.Italic(nameMessage.Content)}\"?", "Are you sure?");
            var confirmationResponseBuilder = new DiscordMessageBuilder()
                .WithEmbed(confirmationEmbed)
                .AddComponents(Interactions.Pets.NameConfirm, Interactions.Pets.NameRetry);

            var confirmationMessage = await nameMessage.RespondAsync(confirmationResponseBuilder);
            var confirmResult = await confirmationMessage.WaitForButtonAsync(nameMessage.Author);

            if (!confirmResult.TimedOut)
            {
                confirmationResponseBuilder.ClearComponents();
                await confirmResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(confirmationResponseBuilder));
                named = confirmResult.Result.Id == InteractionIds.Pets.NameConfirm;
            }
            else
            {
                await confirmationMessage.DeleteAsync();
                await nameMessage.RespondAsync(GetNamingTimedOutMessage(pet));
                named = true;
            }
            return named;
        }

        public bool HasSpaceForAnotherPet(DiscordMember user)
        {
            var capacity = GetPetCapacity(user.Guild.Id, user.Id);
            var ownedPets = GetNumberOfOwnedPets(user.Id);

            return ownedPets < capacity;
        }

        private static string GetAge(DateTime birthdate)
        {
            var age = DateTime.UtcNow - birthdate;
            var ageStr = age.Humanize(maxUnit: Humanizer.Localisation.TimeUnit.Year);
            return string.Concat(ageStr, " old");
        }

        private double GetSearchSuccessProbability(DiscordMember userSearching)
        {
            var ownedPets = (double)GetNumberOfOwnedPets(userSearching.Id);
            var probability = 2 / ownedPets;
            return Math.Min(1, probability);
        }

        private double GetBefriendSuccessProbability(DiscordMember user, Pet target)
        {
            const double baseRate = 0.1;

            var rarityModifier = RandomNumberGenerator.GetInt32((int)target.Rarity+1);
            var petCapacity = (double)GetPetCapacity(user.Guild.Id, user.Id);
            var ownedPets = (double)GetNumberOfOwnedPets(user.Id);
            return baseRate + ((petCapacity - ownedPets) / (petCapacity + rarityModifier));
        }

        private int GetPetCapacity(ulong guildId, ulong userId)
        {
            int result = 1;
            const int newPetSlotUnlockLevels = 20;

            if(Cache.Users.TryGetUser(guildId, userId, out var user))
            {
                result += (user.CurrentLevel / newPetSlotUnlockLevels);
            }
            return result;
        }

        private int GetNumberOfOwnedPets(ulong userId)
        {
            int result = 0;
            if(Cache.Pets.TryGetUsersPets(userId, out var pets))
            {
                result = pets.Count;
            }
            return result;
        }
    }
}
