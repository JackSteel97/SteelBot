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
    public class CommandStatisticProvider
    {
        private readonly ILogger<CommandStatisticProvider> Logger;
        private readonly IDbContextFactory<SteelBotContext> DbContextFactory;
        private readonly AppConfigurationService AppConfigurationService;

        private Dictionary<string, CommandStatistic> StatisticsByCommandName;

        public CommandStatisticProvider(ILogger<CommandStatisticProvider> logger, IDbContextFactory<SteelBotContext> contextFactory, AppConfigurationService appConfigurationService)
        {
            Logger = logger;
            DbContextFactory = contextFactory;
            AppConfigurationService = appConfigurationService;

            StatisticsByCommandName = new Dictionary<string, CommandStatistic>();
            LoadCommandStatisticData();
        }

        private void LoadCommandStatisticData()
        {
            Logger.LogInformation("Loading data from database: Command Statistics");
            using (var db = DbContextFactory.CreateDbContext())
            {
                StatisticsByCommandName = db.CommandStatistics.AsNoTracking().ToDictionary(cs => cs.CommandName);
            }
        }

        private async Task CreateStatistic(string commandName)
        {
            Logger.LogInformation($"Writing a new CommandStatistic [{commandName}] to the database.");
            CommandStatistic cStat = new CommandStatistic(commandName);
            int writtenCount;
            using (var db = DbContextFactory.CreateDbContext())
            {
                db.CommandStatistics.Add(cStat);
                writtenCount = await db.SaveChangesAsync();
            }

            if (writtenCount > 0)
            {
                StatisticsByCommandName.Add(cStat.CommandName, cStat);
            }
            else
            {
                Logger.LogError($"Writing Command Statistic [{cStat.CommandName}] to the database inserted no entities. The internal cache was not changed.");
            }
        }

        private async Task UpdateStatistic(CommandStatistic commandStatistic)
        {
            int writtenCount;
            using (var db = DbContextFactory.CreateDbContext())
            {
                db.CommandStatistics.Update(commandStatistic);
                writtenCount = await db.SaveChangesAsync();
            }

            if (writtenCount > 0)
            {
                StatisticsByCommandName[commandStatistic.CommandName] = commandStatistic;
            }
            else
            {
                Logger.LogError($"Updating CommandStatistic [{commandStatistic.CommandName}] did not alter any entities. The internal cache was not changed.");
            }
        }

        public async Task IncrementCommandStatistic(string commandName)
        {
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