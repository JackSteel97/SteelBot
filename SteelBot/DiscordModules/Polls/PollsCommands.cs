using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SteelBot.Database.Models;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
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
    public class PollsCommands : TypingCommandModule
    {
        private readonly DataHelpers DataHelper;

        public PollsCommands(DataHelpers dataHelper)
        {
            DataHelper = dataHelper;
        }

        private async Task<long> StartGenericPoll(CommandContext context, string title, string[] options, bool lockedPoll)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("No poll title provided."));
                return -1;
            }
            if (options.Length == 0)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("No poll options provided."));
                return -1;
            }
            if (options.Length > 10)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("A poll cannot have more than 10 options."));
                return -1;
            }
            if (Array.Find(options, opt => opt.Length > 255) != null)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("An option cannot be more than 255 characters long."));
                return -1;
            }

            DiscordEmbedBuilder builder = PollsDataHelper.GeneratePollEmbedBuilder(title, options, context.User, DataHelper.Config.GetPrefix(context.Guild.Id), out DiscordEmoji[] reactions);

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
                    // Update message with id.
                    DiscordEmbedBuilder builderWithId = PollsDataHelper.GeneratePollEmbedBuilder(title, options, context.User, DataHelper.Config.GetPrefix(context.Guild.Id), out _, pollId.ToString());

                    await sentMessage.ModifyAsync(embed: builderWithId.Build());
                }
            }

            return pollId;
        }

        [Command("StartTimed")]
        [Description("Starts a poll that closes after a set time.\nDuration format: `XhYmZs`")]
        [Cooldown(2, 120, CooldownBucketType.Channel)]
        public async Task StartTimedPoll(CommandContext context, TimeSpan duration, string title, params string[] options)
        {
            if (duration > TimeSpan.FromHours(24))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error($"The maximum duration of a timed poll is 24 hours."));
                return;
            }

            long pollId = await StartGenericPoll(context, title, options, true);

            if (pollId >= 0)
            {
                // Poll created successfully.
                _ = Task.Run(async () =>
                {
                    // Wait for duration.
                    await Task.Delay(duration);
                    if (DataHelper.Polls.TryGetPoll(pollId, out Poll poll))
                    {
                        await DataHelper.Polls.ClosePoll(poll, context.Channel, context.Client.CurrentUser);
                    }
                });
            }
        }

        [Command("StartLocked")]
        [Description("Starts a poll that only the creator can add options to.")]
        [Cooldown(2, 120, CooldownBucketType.Channel)]
        public async Task StartLockedPoll(CommandContext context, string title, params string[] options)
        {
            await StartGenericPoll(context, title, options, true);
        }

        [Command("Start")]
        [Description("Starts a poll with up to 10 options")]
        [Cooldown(2, 120, CooldownBucketType.Channel)]
        public async Task StartPoll(CommandContext context, string title, params string[] options)
        {
            await StartGenericPoll(context, title, options, false);
        }

        [Command("AddOption")]
        [Aliases("add", "a")]
        [Description("Adds an option to the specified poll.")]
        [Cooldown(10, 120, CooldownBucketType.User)]
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
            if (poll.Options.Exists(opt => opt.OptionText.Equals(newOption, StringComparison.OrdinalIgnoreCase)))
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning($"This poll already has an option for {newOption}."));
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
            DiscordEmbedBuilder builder = PollsDataHelper.GeneratePollEmbedBuilder(updatedPoll.Title, updatedPoll.Options.ConvertAll(opt => opt.OptionText).ToArray(),
                pollAuthor, DataHelper.Config.GetPrefix(context.Guild.Id), out DiscordEmoji[] reactions, pollId.ToString());

            var channel = await context.Client.GetChannelAsync(updatedPoll.ChannelId);
            var message = await channel.GetMessageAsync(updatedPoll.MessageId);

            await message.ModifyAsync(embed: builder.Build());

            await message.DeleteReactionsEmojiAsync(reactions[^1]);

            await message.CreateReactionAsync(reactions[^2]);
            await message.CreateReactionAsync(reactions[^1]);

            await context.RespondAsync(embed: EmbedGenerator.Success($"Added Option **{newOption}** to Poll **{pollId}**"));
        }

        [Command("RemoveOption")]
        [Aliases("remove", "delete", "rm")]
        [Description("Removes an option from the specified poll.")]
        [Cooldown(10, 60, CooldownBucketType.User)]
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
            if (!PollsDataHelper.UserHasEditPermissionOnPoll(poll, context.Member, context.Channel, context.Client.CurrentUser.Id))
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
            DiscordEmbedBuilder builder = PollsDataHelper.GeneratePollEmbedBuilder(updatedPoll.Title, updatedPoll.Options.ConvertAll(opt => opt.OptionText).ToArray(),
                pollAuthor, DataHelper.Config.GetPrefix(context.Guild.Id), out DiscordEmoji[] reactions, pollId.ToString());

            var channel = context.Guild.GetChannel(updatedPoll.ChannelId);
            var message = await channel.GetMessageAsync(updatedPoll.MessageId);

            await message.ModifyAsync(embed: builder.Build());

            await message.DeleteReactionsEmojiAsync(reactionToRemove);
            await context.RespondAsync(embed: EmbedGenerator.Success($"Removed Option **{optionToRemove}** from Poll **{pollId}**"));
        }
    }
}