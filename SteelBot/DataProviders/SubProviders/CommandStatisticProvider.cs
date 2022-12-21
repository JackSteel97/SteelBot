using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SteelBot.Database;
using SteelBot.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders;

public class CommandStatisticProvider
{
    private readonly IDbContextFactory<SteelBotContext> _dbContextFactory;
    private readonly ILogger<CommandStatisticProvider> _logger;

    private Dictionary<string, CommandStatistic> _statisticsByCommandName;

    public CommandStatisticProvider(ILogger<CommandStatisticProvider> logger, IDbContextFactory<SteelBotContext> contextFactory)
    {
        _logger = logger;
        _dbContextFactory = contextFactory;

        _statisticsByCommandName = new Dictionary<string, CommandStatistic>();
        LoadCommandStatisticData();
    }

    private void LoadCommandStatisticData()
    {
        _logger.LogInformation("Loading data from database: Command Statistics");
        using (var db = _dbContextFactory.CreateDbContext())
        {
            _statisticsByCommandName = db.CommandStatistics.AsNoTracking().ToDictionary(cs => cs.CommandName);
        }
    }

    private async Task CreateStatistic(string commandName)
    {
        _logger.LogInformation($"Writing a new CommandStatistic [{commandName}] to the database.");
        var cStat = new CommandStatistic(commandName);

        int writtenCount;
        await using (var db = _dbContextFactory.CreateDbContext())
        {
            db.CommandStatistics.Add(cStat);
            writtenCount = await db.SaveChangesAsync();
        }

        if (writtenCount > 0)
            _statisticsByCommandName.Add(cStat.CommandName, cStat);
        else
            _logger.LogError($"Writing Command Statistic [{cStat.CommandName}] to the database inserted no entities. The internal cache was not changed.");
    }

    private async Task UpdateStatistic(CommandStatistic commandStatistic)
    {
        int writtenCount;
        await using (var db = _dbContextFactory.CreateDbContext())
        {
            db.CommandStatistics.Update(commandStatistic);
            writtenCount = await db.SaveChangesAsync();
        }

        if (writtenCount > 0)
            _statisticsByCommandName[commandStatistic.CommandName] = commandStatistic;
        else
            _logger.LogError($"Updating CommandStatistic [{commandStatistic.CommandName}] did not alter any entities. The internal cache was not changed.");
    }

    public async Task IncrementCommandStatistic(string commandName)
    {
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
    }

    public bool CommandStatisticExists(string commandName) => _statisticsByCommandName.ContainsKey(commandName);

    public List<CommandStatistic> GetAllCommandStatistics() => _statisticsByCommandName.Values.ToList();
}