using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SteelBot.Database;
using SteelBot.Database.Models;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders
{
    public class PollsProvider
    {
        private readonly ILogger<PollsProvider> Logger;
        private readonly IDbContextFactory<SteelBotContext> DbContextFactory;

        private readonly Dictionary<ulong, Poll> PollsByMessageId;
        private readonly Dictionary<long, Poll> PollsByPollId;

        public PollsProvider(ILogger<PollsProvider> logger, IDbContextFactory<SteelBotContext> contextFactory)
        {
            Logger = logger;
            DbContextFactory = contextFactory;

            PollsByMessageId = new Dictionary<ulong, Poll>();
            PollsByPollId = new Dictionary<long, Poll>();
            LoadPollData();
        }

        private void LoadPollData()
        {
            Logger.LogInformation("Loading data from database: Polls");
            Poll[] allPolls;
            using (var db = DbContextFactory.CreateDbContext())
            {
                allPolls = db.Polls.AsNoTracking()
                .Include(p => p.Options)
                .Include(p => p.PollCreator)
                .ToArray();
            }

            foreach (Poll poll in allPolls)
            {
                AddPollToInternalCache(poll);
            }
        }

        private void AddPollToInternalCache(Poll poll)
        {
            PollsByMessageId.Add(poll.MessageId, poll);
            PollsByPollId.Add(poll.RowId, poll);
        }

        private void RemovePollFromInternalCache(Poll poll)
        {
            PollsByMessageId.Remove(poll.MessageId);
            PollsByPollId.Remove(poll.RowId);
        }

        private void UpdatePollInInternalCache(Poll newPoll)
        {
            PollsByMessageId[newPoll.MessageId] = newPoll;
            PollsByPollId[newPoll.RowId] = newPoll;
        }

        public bool BotKnowsPoll(ulong messageId)
        {
            return PollsByMessageId.ContainsKey(messageId);
        }

        public bool TryGetPoll(ulong messageId, out Poll poll)
        {
            return PollsByMessageId.TryGetValue(messageId, out poll);
        }

        public bool TryGetPoll(long pollId, out Poll poll)
        {
            return PollsByPollId.TryGetValue(pollId, out poll);
        }

        public async Task AddOptionToPoll(Poll poll, string newOption)
        {
            Logger.LogInformation($"Adding Option [{newOption}] to Poll [{poll.RowId}]");

            PollOption pollOption = new PollOption(newOption, poll.Options.Count + 1)
            {
                PollRowId = poll.RowId
            };

            await InsertNewPollOption(poll.Clone(), pollOption);
        }

        public async Task RemoveOptionFromPoll(Poll poll, string optionToRemove)
        {
            Logger.LogInformation($"Removing Option [{optionToRemove}] from Poll [{poll.RowId}]");

            var pollClone = poll.Clone();
            var optionToDeleteIndex = poll.Options.FindIndex(opt => opt.OptionText.Equals(optionToRemove, StringComparison.OrdinalIgnoreCase));
            if (optionToDeleteIndex >= 0)
            {
                var optionToDelete = poll.Options[optionToDeleteIndex];
                await DeletePollOption(pollClone, optionToDelete, optionToDeleteIndex);
            }
        }

        public async Task InsertPoll(Poll poll)
        {
            Logger.LogInformation($"Writing a new Poll [{poll.Title}] for Message [{poll.MessageId}] to the database.");

            int writtenCount;
            using (var db = DbContextFactory.CreateDbContext())
            {
                db.Polls.Add(poll);
                writtenCount = await db.SaveChangesAsync();
            }

            if (writtenCount > 0)
            {
                AddPollToInternalCache(poll);
            }
            else
            {
                Logger.LogError($"Writing Poll [{poll.Title}] for Message [{poll.MessageId}] to the database inserted no entities. The internal cache was not changed.");
            }
        }

        public async Task DeletePoll(Poll poll)
        {
            Logger.LogInformation($"Deleting a Poll [{poll.Title}] for Message [{poll.MessageId}] from the database.");

            int writtenCount;
            using (var db = DbContextFactory.CreateDbContext())
            {
                Poll original = db.Polls.First(u => u.RowId == poll.RowId);
                db.Polls.Remove(original);
                writtenCount = await db.SaveChangesAsync();
            }

            if (writtenCount > 0)
            {
                RemovePollFromInternalCache(poll);
            }
            else
            {
                Logger.LogError($"Deleting Poll [{poll.Title}] for Message [{poll.MessageId}] to the database removed no entities. The internal cache was not changed.");
            }
        }

        public async Task UpdatePoll(Poll newPoll)
        {
            int writtenCount;
            using (var db = DbContextFactory.CreateDbContext())
            {
                // To prevent EF tracking issue, grab and alter existing value.
                Poll original = db.Polls.First(u => u.RowId == newPoll.RowId);
                db.Entry(original).CurrentValues.SetValues(newPoll);
                writtenCount = await db.SaveChangesAsync();
            }

            if (writtenCount > 0)
            {
                UpdatePollInInternalCache(newPoll);
            }
            else
            {
                Logger.LogError($"Updating Poll [{newPoll.Title}] for Message [{newPoll.MessageId}] did not alter any entities. The internal cache was not changed.");
            }
        }

        private async Task InsertNewPollOption(Poll poll, PollOption pollOption)
        {
            Logger.LogInformation($"Inserting a new Option [{pollOption.OptionText}] for Poll [{poll.Title}] to the database.");
            int writtenCount;
            using (var db = DbContextFactory.CreateDbContext())
            {
                db.PollOptions.Add(pollOption);
                writtenCount = await db.SaveChangesAsync();
            }

            if (writtenCount > 0)
            {
                poll.Options.Add(pollOption);
                UpdatePollInInternalCache(poll);
            }
            else
            {
                Logger.LogError($"Inserting a new Option [{pollOption.OptionText}] for Poll [{poll.Title}] did not insert any entities. The internal cache was not changed.");
            }
        }

        private async Task DeletePollOption(Poll poll, PollOption pollOption, int optionIndex)
        {
            Logger.LogInformation($"Deleting Option [{pollOption.OptionText}] for Poll [{poll.Title}] from the database.");

            int writtenCount;
            using (var db = DbContextFactory.CreateDbContext())
            {
                db.PollOptions.Remove(pollOption);
                writtenCount = await db.SaveChangesAsync();
            }

            if (writtenCount > 0)
            {
                poll.Options.RemoveAt(optionIndex);
                UpdatePollInInternalCache(poll);
            }
            else
            {
                Logger.LogError($"Inserting a new Option [{pollOption.OptionText}] for Poll [{poll.Title}] did not insert any entities. The internal cache was not changed.");
            }
        }
    }
}