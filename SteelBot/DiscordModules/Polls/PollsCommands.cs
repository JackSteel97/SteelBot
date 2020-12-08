using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SteelBot.Database.Models;
using SteelBot.Helpers;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Polls
{
    [Group("Polls")]
    [Aliases("poll", "p")]
    [Description("Commands for starting and editing polls.")]
    [RequireGuild]
    public class PollsCommands : BaseCommandModule
    {
        private readonly DataHelpers DataHelper;

        public PollsCommands(DataHelpers dataHelper)
        {
            DataHelper = dataHelper;
        }

        private async Task StartGenericPoll(CommandContext context, string title, string[] options, bool lockedPoll)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("No poll title provided."));
                return;
            }
            if (options.Length == 0)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("No poll options provided."));
                return;
            }
            if (options.Length > 10)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("A poll cannot have more than 10 options."));
                return;
            }
            if (Array.Find(options, opt => opt.Length > 255) != null)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("An option cannot be more than 255 characters long."));
                return;
            }

            (DiscordEmbedBuilder builder, StringBuilder optionBuilder) = PollsDataHelper.GeneratePollEmbedBuilder(title, options, context.User, out DiscordEmoji[] reactions);

            var sentMessage = await context.RespondAsync(embed: builder.Build());

            foreach (var reaction in reactions)
            {
                await sentMessage.CreateReactionAsync(reaction);
            }

            (bool success, string errorMessage) = await DataHelper.Polls.CreatePoll(title, options, context.Guild.Id, context.User.Id, sentMessage.Id, context.Channel.Id, lockedPoll);
            if (!success)
            {
                await context.Message.RespondAsync(embed: EmbedGenerator.Error(errorMessage));
            }

            if (DataHelper.Polls.TryGetPollId(sentMessage.Id, out long pollId))
            {
                if (options.Length < 10)
                {
                    // Update message with id if someone could add options.
                    optionBuilder.AppendLine($"Poll Id: **{pollId}**");
                    optionBuilder.AppendLine($"Use `{DataHelper.Config.GetPrefix(context.Guild.Id)}AddOption {pollId} \"OptionText\"` to add another option to this poll.");
                    builder.WithDescription(optionBuilder.ToString());

                    await sentMessage.ModifyAsync(embed: builder.Build());
                }
            }
        }

        [Command("StartLocked")]
        [Description("Starts a poll that only the creator can add options to.")]
        public async Task StartLockedPoll(CommandContext context, string title, params string[] options)
        {
            await StartGenericPoll(context, title, options, true);
        }

        [Command("Start")]
        [Description("Starts a poll with up to 10 options")]
        public async Task StartPoll(CommandContext context, string title, params string[] options)
        {
            await StartGenericPoll(context, title, options, false);
        }

        [Command("AddOption")]
        [Description("Adds an option to the specified poll.")]
        public async Task AddOption(CommandContext context, long pollId, [RemainingText] string newOption)
        {
            if (string.IsNullOrWhiteSpace(newOption))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("No option provided."));
                return;
            }
            if (!DataHelper.Polls.TryGetPoll(pollId, out Poll poll))
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning("This poll does not exist, make sure you entered the Poll Id correctly."));
                return;
            }
            if (poll.Options.Count >= 10)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("Max number of options per poll already reached."));
                return;
            }
            if (newOption.Length > 255)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("An option cannot be more than 255 characters long."));
                return;
            }
            if (poll.IsLockedPoll)
            {
                if (!PollsDataHelper.UserHasEditPermissionOnPoll(poll, context.Member, context.Channel, context.Client.CurrentUser.Id))
                {
                    await context.RespondAsync(embed: EmbedGenerator.Warning("This poll is locked, only the creator can add options."));
                    return;
                }
            }

            Poll updatedPoll = await DataHelper.Polls.AddOptionToPoll(poll, newOption);

            var pollAuthor = await context.Client.GetUserAsync(updatedPoll.PollCreator.DiscordId);
            (DiscordEmbedBuilder builder, StringBuilder optionBuilder) = PollsDataHelper.GeneratePollEmbedBuilder(updatedPoll.Title, updatedPoll.Options.ConvertAll(opt => opt.OptionText).ToArray(),
                pollAuthor, out DiscordEmoji[] reactions);

            var channel = await context.Client.GetChannelAsync(updatedPoll.ChannelId);
            var message = await channel.GetMessageAsync(updatedPoll.MessageId);

            if (updatedPoll.Options.Count < 10)
            {
                // Update message with id if someone could add options.
                optionBuilder.AppendLine($"Poll Id: **{pollId}**");
                optionBuilder.AppendLine($"Use `{DataHelper.Config.GetPrefix(context.Guild.Id)}Polls AddOption {pollId} \"OptionText\"` to add another option to this poll.");
                builder.WithDescription(optionBuilder.ToString());
            }

            await message.ModifyAsync(embed: builder.Build());

            await message.DeleteReactionsEmojiAsync(reactions[^1]);

            await message.CreateReactionAsync(reactions[^2]);
            await message.CreateReactionAsync(reactions[^1]);

            await context.RespondAsync(embed: EmbedGenerator.Success($"Added Option **{newOption}** to Poll **{pollId}**"));
        }

        [Command("RemoveOption")]
        [Description("Removes an option from the specified poll.")]
        public async Task RemoveOption(CommandContext context, long pollId, [RemainingText] string optionToRemove)
        {
            if (string.IsNullOrWhiteSpace(optionToRemove))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("No option provided."));
                return;
            }
            if (!DataHelper.Polls.TryGetPoll(pollId, out Poll poll))
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning("This poll does not exist, make sure you entered the Poll Id correctly."));
                return;
            }
            if (poll.Options.Count <= 1)
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning("You cannot remove the last option on a poll."));
                return;
            }
            if (context.User.Id != poll.PollCreator.DiscordId)
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning("Only the poll creator can remove options."));
                return;
            }

            var pollOption = poll.Options.Find(opt => opt.OptionText.Equals(optionToRemove, StringComparison.OrdinalIgnoreCase));
            if (pollOption == null)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error($"Poll **{pollId}** does not have an option **{optionToRemove}**."));
                return;
            }

            var reactionToRemove = DiscordEmoji.FromUnicode(EmojiHelper.GetNumberEmoji(pollOption.OptionNumber));

            Poll updatedPoll = await DataHelper.Polls.RemoveOptionFromPoll(poll, optionToRemove);

            var pollAuthor = await context.Client.GetUserAsync(updatedPoll.PollCreator.DiscordId);
            (DiscordEmbedBuilder builder, StringBuilder optionBuilder) = PollsDataHelper.GeneratePollEmbedBuilder(updatedPoll.Title, updatedPoll.Options.ConvertAll(opt => opt.OptionText).ToArray(),
                pollAuthor, out DiscordEmoji[] reactions);

            var channel = context.Guild.GetChannel(updatedPoll.ChannelId);
            var message = await channel.GetMessageAsync(updatedPoll.MessageId);

            if (updatedPoll.Options.Count < 10)
            {
                // Update message with id if someone could add options.
                optionBuilder.AppendLine($"Poll Id: **{pollId}**");
                optionBuilder.AppendLine($"Use `{DataHelper.Config.GetPrefix(context.Guild.Id)}AddOption {pollId} \"OptionText\"` to add another option to this poll.");
                builder.WithDescription(optionBuilder.ToString());
            }

            await message.ModifyAsync(embed: builder.Build());

            await message.DeleteReactionsEmojiAsync(reactionToRemove);
            await context.RespondAsync(embed: EmbedGenerator.Success($"Removed Option **{optionToRemove}** from Poll **{pollId}**"));
        }
    }
}