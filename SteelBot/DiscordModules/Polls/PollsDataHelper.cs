using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using SteelBot.Database.Models;
using SteelBot.DataProviders;
using SteelBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Polls
{
    public class PollsDataHelper
    {
        private readonly DataCache Cache;

        public PollsDataHelper(DataCache cache)
        {
            Cache = cache;
        }

        public async Task<(bool success, string errorMessage)> CreatePoll(string title, string[] options, ulong guildId, ulong authorId, ulong messageId, ulong channelId, bool lockedPoll = false)
        {
            if (!Cache.Users.TryGetUser(guildId, authorId, out User user))
            {
                return (false, "Could not find poll creator.");
            }

            if (Cache.Polls.BotKnowsPoll(messageId))
            {
                return (false, "Cannot create duplicate poll.");
            }

            Poll poll = new Poll(title, user.RowId, messageId, options.ToList(), channelId, lockedPoll);
            await Cache.Polls.InsertPoll(poll, user);
            return (true, null);
        }

        public bool TryGetPollId(ulong messageId, out long id)
        {
            if (Cache.Polls.TryGetPoll(messageId, out Poll poll))
            {
                id = poll.RowId;
                return true;
            }
            id = -1;
            return false;
        }

        public bool TryGetPoll(long pollId, out Poll poll)
        {
            return Cache.Polls.TryGetPoll(pollId, out poll);
        }

        public async Task<Poll> AddOptionToPoll(Poll poll, string newOption)
        {
            await Cache.Polls.AddOptionToPoll(poll, newOption);
            Cache.Polls.TryGetPoll(poll.RowId, out Poll updatedPoll);
            return updatedPoll;
        }

        public async Task<Poll> RemoveOptionFromPoll(Poll poll, string optionToRemove)
        {
            await Cache.Polls.RemoveOptionFromPoll(poll, optionToRemove);
            Cache.Polls.TryGetPoll(poll.RowId, out Poll updatedPoll);
            return updatedPoll;
        }

        public static DiscordEmbedBuilder GeneratePollEmbedBuilder(string title, string[] options, DiscordUser creator, string commandPrefix, out DiscordEmoji[] reactions, string pollId = null)
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder().WithColor(EmbedGenerator.PrimaryColour)
               .WithAuthor(creator.Username, null, creator.AvatarUrl)
               .WithTitle(title)
               .WithFooter($"React with {EmojiConstants.Objects.StopSign} to close the poll");

            reactions = new DiscordEmoji[options.Length + 1];
            StringBuilder optionString = new StringBuilder();
            for (int i = 0; i < options.Length; i++)
            {
                string emoji = EmojiHelper.GetNumberEmoji(i + 1);
                optionString.AppendLine($"{emoji} - {options[i]}\n");
                reactions[i] = DiscordEmoji.FromUnicode(emoji);
            }
            reactions[options.Length] = DiscordEmoji.FromUnicode(EmojiConstants.Objects.StopSign);

            optionString.AppendLine($"Poll Id: **{pollId ?? EmojiConstants.CustomDiscordEmojis.LoadingSpinner}**");

            if (!string.IsNullOrWhiteSpace(pollId))
            {
                if (options.Length < 10)
                {
                    // Update message if someone could add options.
                    optionString.AppendLine($"Use `{commandPrefix}Poll Add {pollId} <OptionText>` to add another option to this poll.");
                }
                else
                {
                    // Update message if someone could remove options.
                    optionString.AppendLine($"Use `{commandPrefix}Poll Remove {pollId} <OptionText>` to remove an option from this poll.");
                }
            }

            builder.WithDescription(optionString.ToString());
            return builder;
        }

        public static bool UserHasEditPermissionOnPoll(Poll poll, DiscordMember user, DiscordChannel channel, ulong botUserId)
        {
            if (poll == null || user == null)
            {
                return false;
            }
            Permissions perms = user.PermissionsIn(channel);

            return (user.Id == poll.PollCreator.DiscordId || perms.HasFlag(Permissions.Administrator) || perms.HasFlag(Permissions.ManageMessages))
                && user.Id != botUserId;
        }

        public async Task HandleMessageReaction(MessageReactionAddEventArgs args, ulong botId)
        {
            if (Cache.Polls.TryGetPoll(args.Message.Id, out Poll poll))
            {
                // Poll for this message exists.
                if (args.Emoji.Name == EmojiConstants.Objects.StopSign)
                {
                    var user = args.User as DiscordMember;

                    // It was the stop emoji.
                    if (UserHasEditPermissionOnPoll(poll, user, args.Channel, botId))
                    {
                        // The reaction was done by the poll creator.
                        await ClosePoll(poll, args.Channel, user);
                    }
                    else if (args.User.Id != botId)
                    {
                        DiscordMember discordMember = (DiscordMember)args.User;
                        await discordMember.SendMessageAsync(embed: EmbedGenerator.Warning($"You do not have permission to close poll **{poll.RowId}**"));
                    }
                }
            }
        }

        public async Task ClosePoll(Poll poll, DiscordChannel channel, DiscordUser closer)
        {
            DiscordMessage pollMessage = await channel.GetMessageAsync(poll.MessageId);

            Dictionary<string, PollOption> validReactionOptions = new Dictionary<string, PollOption>();

            foreach (PollOption option in poll.Options)
            {
                validReactionOptions.Add(EmojiHelper.GetNumberEmoji(option.OptionNumber), option);
            }

            List<(int votes, PollOption option)> results = new List<(int, PollOption)>();
            int totalVotes = 0;
            foreach (DiscordReaction reaction in pollMessage.Reactions)
            {
                if (validReactionOptions.TryGetValue(reaction.Emoji.Name, out PollOption option))
                {
                    // Subtract one to avoid counting the initial bot reaction.
                    int votes = reaction.Count - 1;
                    totalVotes += votes;
                    results.Add((votes, option));
                }
            }

            results = results.OrderByDescending(r => r.votes).ToList();

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(EmbedGenerator.InfoColour)
                .WithTitle($"Poll Results!")
                .AddField("Total Votes", totalVotes.ToString(), true)
                .AddField("Closed By", closer.Mention, true);

            StringBuilder resultsBuilder = new StringBuilder();
            resultsBuilder.AppendLine($"**{poll.Title}**\n");
            int place = 1;
            foreach ((int votes, PollOption option) in results)
            {
                if (votes == 0)
                {
                    break;
                }
                double portionOfVote = Math.Round(((double)votes / totalVotes) * 100, 1);
                resultsBuilder.AppendLine($"{EmojiHelper.GetNumberEmoji(place)} - **{option.OptionText}** - {portionOfVote}% ({votes} votes)");
                place++;
            }

            builder.WithDescription(resultsBuilder.ToString());

            await channel.SendMessageAsync(embed: builder.Build());
            await channel.DeleteMessageAsync(pollMessage);
            await Cache.Polls.DeletePoll(poll);
        }
    }
}