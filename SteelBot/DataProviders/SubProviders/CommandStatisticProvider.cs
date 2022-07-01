using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.Database;
using SteelBot.Database.Models;
using SteelBot.Helpers.Sentry;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders
{
    public class CommandStatisticProvider
    {
        private readonly ILogger<CommandStatisticProvider> Logger;
        private readonly IDbContextFactory<SteelBotContext> DbContextFactory;
        private readonly IHub _sentry;

        private Dictionary<string, CommandStatistic> StatisticsByCommandName;

        public CommandStatisticProvider(ILogger<CommandStatisticProvider> logger, IDbContextFactory<SteelBotContext> contextFactory, IHub sentry)
        {
            Logger = logger;
            DbContextFactory = contextFactory;

            StatisticsByCommandName = new Dictionary<string, CommandStatistic>();
            _sentry = sentry;
            LoadCommandStatisticData();
        }

        private void LoadCommandStatisticData()
        {
            Logger.LogInformation("Loading data from database: Command Statistics");
            var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(LoadCommandStatisticData));
            using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                StatisticsByCommandName = db.CommandStatistics.AsNoTracking().ToDictionary(cs => cs.CommandName);
            }

            transaction.Finish();
        }

        private async Task CreateStatistic(string commandName)
        {
            var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(CreateStatistic));
            Logger.LogInformation($"Writing a new CommandStatistic [{commandName}] to the database.");
            CommandStatistic cStat = new CommandStatistic(commandName);

            var dbSpan = transaction.StartChild("Write to Database");
            int writtenCount;
            await using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                db.CommandStatistics.Add(cStat);
                writtenCount = await db.SaveChangesAsync();
            }
            dbSpan.Finish();

            if (writtenCount > 0)
            {
                var cacheSpan = transaction.StartChild("Write to Cache");
                StatisticsByCommandName.Add(cStat.CommandName, cStat);
                cacheSpan.Finish();
            }
            else
            {
                Logger.LogError($"Writing Command Statistic [{cStat.CommandName}] to the database inserted no entities. The internal cache was not changed.");
            }

            transaction.Finish();
        }

        private async Task UpdateStatistic(CommandStatistic commandStatistic)
        {
            var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(UpdateStatistic));

            var dbSpan = transaction.StartChild("Write to Database");
            int writtenCount;
            await using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                db.CommandStatistics.Update(commandStatistic);
                writtenCount = await db.SaveChangesAsync();
            }

            dbSpan.Finish();

            if (writtenCount > 0)
            {
                var cacheSpan = transaction.StartChild("Write to Cache");
                StatisticsByCommandName[commandStatistic.CommandName] = commandStatistic;
                cacheSpan.Finish();
            }
            else
            {
                Logger.LogError($"Updating CommandStatistic [{commandStatistic.CommandName}] did not alter any entities. The internal cache was not changed.");
            }

            transaction.Finish();
        }

        public async Task IncrementCommandStatistic(string commandName)
        {
            var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(IncrementCommandStatistic));

            if (!StatisticsByCommandName.TryGetValue(commandName, out CommandStatistic cStat))
            {
                await CreateStatistic(commandName);
            }
            else
            {
                CommandStatistic cStatCopy = cStat.Clone();
                cStatCopy.RowId = cStat.RowId;
                cStatCopy.UsageCount++;
                cStatCopy.LastUsed = DateTime.UtcNow;
                await UpdateStatistic(cStatCopy);
            }

            transaction.Finish();
        }

        public bool CommandStatisticExists(string commandName)
        {
            return StatisticsByCommandName.ContainsKey(commandName);
        }

        public List<CommandStatistic> GetAllCommandStatistics()
        {
            return StatisticsByCommandName.Values.ToList();
        }
    }
}