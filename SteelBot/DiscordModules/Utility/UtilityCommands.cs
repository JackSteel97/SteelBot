using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using SteelBot.Attributes;
using SteelBot.Helpers;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Utility
{
    [Group("Utility")]
    [Description("Helpful functions.")]
    [Aliases("util", "u")]
    public class UtilityCommands : BaseCommandModule
    {
        private readonly Random Rand;

        public UtilityCommands()
        {
            Rand = new Random();
        }

        [Command("Ping")]
        [Description("Pings the bot.")]
        public Task Ping(CommandContext context)
        {
            string ret = DateTime.UtcNow.Millisecond % 5 == 0 ? "POG!" : "PONG!";
            return context.RespondAsync(embed: EmbedGenerator.Primary("", ret));
        }

        [Command("Choose")]
        [Aliases("PickFrom", "Pick", "Select", "pf")]
        [Description("Select x options randomly from a given list.")]
        public Task Choose(CommandContext context, int numberToSelect, params string[] options)
        {
            // Validation.
            if (numberToSelect <= 0)
            {
                return context.RespondAsync(embed: EmbedGenerator.Error("X must be greater than zero."));
            }
            if (options.Length == 0)
            {
                return context.RespondAsync(embed: EmbedGenerator.Error("No options were provided."));
            }
            if (numberToSelect > options.Length)
            {
                return context.RespondAsync(embed: EmbedGenerator.Error($"There are not enough options to choose {numberToSelect} unique options.\nPlease provide more options or choose less."));
            }
            List<string> remainingOptions = options.ToList();
            List<string> selectedOptions = new List<string>(numberToSelect);
            for (int i = 0; i < numberToSelect; i++)
            {
                // Pick random option.
                int randIndex = Rand.Next(remainingOptions.Count);
                selectedOptions.Add(remainingOptions[randIndex]);
                // Remove from possible options.
                remainingOptions.RemoveAt(randIndex);
            }
            return context.RespondAsync(embed: EmbedGenerator.Primary(string.Join(", ", selectedOptions), $"Chosen Option{(numberToSelect > 1 ? "s" : "")}"));
        }

        [Command("FlipCoin")]
        [Aliases("TossCoin", "fc")]
        [Description("Flips a coin.")]
        public Task FlipCoin(CommandContext context)
        {
            int side = Rand.Next(100);
            string result = "Heads!";
            if (side < 50)
            {
                result = "Tails!";
            }
            return context.RespondAsync(embed: EmbedGenerator.Primary(result));
        }

        [Command("RollDie")]
        [Aliases("Roll", "rd")]
        [Description("Rolls a die.")]
        public Task RollDie(CommandContext context, int sides = 6)
        {
            int rolledNumber = Rand.Next(1, sides + 1);
            return context.RespondAsync(embed: EmbedGenerator.Primary($"{context.User.Mention} rolled {rolledNumber}"));
        }

        [Command("Speak")]
        [Description("Get the bot to post the given message in a channel.")]
        [RequireUserPermissions(Permissions.Administrator)]
        public Task Speak(CommandContext context, DiscordChannel channel, string title, string content, string footerContent = "")
        {
            if (!context.Guild.Channels.ContainsKey(channel.Id))
            {
                return context.RespondAsync(embed: EmbedGenerator.Error("The channel specified does not exist in this server."));
            }
            if (channel.Type != ChannelType.Text)
            {
                return context.RespondAsync(embed: EmbedGenerator.Error("The channel specified is not a text channel."));
            }
            if (string.IsNullOrWhiteSpace(title))
            {
                return context.RespondAsync(embed: EmbedGenerator.Error("No valid message title was provided."));
            }
            if (string.IsNullOrWhiteSpace(content))
            {
                return context.RespondAsync(embed: EmbedGenerator.Error("No valid message content was provided."));
            }

            return channel.SendMessageAsync(embed: EmbedGenerator.Info(content, title, footerContent));
        }
    }
}