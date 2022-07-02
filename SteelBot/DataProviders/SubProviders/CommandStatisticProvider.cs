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

namespace SteelBot.DataProviders.SubProviders;

public class CommandStatisticProvider
{
    private readonly ILogger<CommandStatisticProvider> _logger;
    private readonly IDbContextFactory<SteelBotContext> _dbContextFactory;
    private readonly IHub _sentry;

    private Dictionary<string, CommandStatistic> _statisticsByCommandName;

    public CommandStatisticProvider(ILogger<CommandStatisticProvider> logger, IDbContextFactory<SteelBotContext> contextFactory, IHub sentry)
    {
        _logger = logger;
        _dbContextFactory = contextFactory;

        _statisticsByCommandName = new Dictionary<string, CommandStatistic>();
        _sentry = sentry;
        LoadCommandStatisticData();
    }

    private void LoadCommandStatisticData()
    {
        var transaction = _sentry.StartNewConfiguredTransaction("StartUp", nameof(LoadCommandStatisticData));

        _logger.LogInformation("Loading data from database: Command Statistics");
        using (var db = _dbContextFactory.CreateDbContext())
        {
            _statisticsByCommandName = db.CommandStatistics.AsNoTracking().ToDictionary(cs => cs.CommandName);
        }

        transaction.Finish();
    }

    private async Task CreateStatistic(string commandName)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(CreateStatistic));
        _logger.LogInformation($"Writing a new CommandStatistic [{commandName}] to the database.");
        var cStat = new CommandStatistic(commandName);

        var dbSpan = transaction.StartChild("Write to Database");
        int writtenCount;
        await using (var db = _dbContextFactory.CreateDbContext())
        {
            db.CommandStatistics.Add(cStat);
            writtenCount = await db.SaveChangesAsync();
        }
        dbSpan.Finish();

        if (writtenCount > 0)
        {
            var cacheSpan = transaction.StartChild("Write to Cache");
            _statisticsByCommandName.Add(cStat.CommandName, cStat);
            cacheSpan.Finish();
        }
        else
        {
            _logger.LogError($"Writing Command Statistic [{cStat.CommandName}] to the database inserted no entities. The internal cache was not changed.");
        }

        transaction.Finish();
    }

    private async Task UpdateStatistic(CommandStatistic commandStatistic)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(UpdateStatistic));

        var dbSpan = transaction.StartChild("Write to Database");
        int writtenCount;
        await using (var db = _dbContextFactory.CreateDbContext())
        {
            db.CommandStatistics.Update(commandStatistic);
            writtenCount = await db.SaveChangesAsync();
        }

        dbSpan.Finish();

        if (writtenCount > 0)
        {
            var cacheSpan = transaction.StartChild("Write to Cache");
            _statisticsByCommandName[commandStatistic.CommandName] = commandStatistic;
            cacheSpan.Finish();
        }
        else
        {
            _logger.LogError($"Updating CommandStatistic [{commandStatistic.CommandName}] did not alter any entities. The internal cache was not changed.");
        }

        transaction.Finish();
    }

    public async Task IncrementCommandStatistic(string commandName)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(IncrementCommandStatistic));

        if (!_statisticsByCommandName.TryGetValue(commandName, out var cStat))
        {
            await CreateStatistic(commandName);
        }
        else
        {
            var cStatCopy = cStat.Clone();
            cStatCopy.RowId = cStat.RowId;
            cStatCopy.UsageCount++;
            cStatCopy.LastUsed = DateTime.UtcNow;
            await UpdateStatistic(cStatCopy);
        }

        transaction.Finish();
    }

    public bool CommandStatisticExists(string commandName) => _statisticsByCommandName.ContainsKey(commandName);

    public List<CommandStatistic> GetAllCommandStatistics() => _statisticsByCommandName.Values.ToList();
}