using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.Database;
using SteelBot.Database.Models;
using SteelBot.Helpers.Sentry;
using SteelBot.Services.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders;

public class GuildsProvider
{
    private readonly ILogger<GuildsProvider> _logger;
    private readonly IDbContextFactory<SteelBotContext> _dbContextFactory;
    private readonly AppConfigurationService _appConfigurationService;
    private readonly IHub _sentry;

    private Dictionary<ulong, Guild> _guildsByDiscordId;

    public GuildsProvider(ILogger<GuildsProvider> logger, IDbContextFactory<SteelBotContext> contextFactory, AppConfigurationService appConfigurationService, IHub sentry)
    {
        _logger = logger;
        _dbContextFactory = contextFactory;
        _appConfigurationService = appConfigurationService;
        _sentry = sentry;

        _guildsByDiscordId = new Dictionary<ulong, Guild>();
        LoadGuildData();
    }

    private void LoadGuildData()
    {
        var transaction = _sentry.StartNewConfiguredTransaction("StartUp", nameof(LoadGuildData));
        _logger.LogInformation("Loading data from database: Guilds");
        using (var db = _dbContextFactory.CreateDbContext())
        {
            _guildsByDiscordId = db.Guilds.AsNoTracking().ToDictionary(g => g.DiscordId);
        }

        transaction.Finish();
    }

    public bool BotKnowsGuild(ulong discordId) => _guildsByDiscordId.ContainsKey(discordId);

    public bool TryGetGuild(ulong discordId, out Guild guild) => _guildsByDiscordId.TryGetValue(discordId, out guild);

    public string GetGuildPrefix(ulong discordId)
    {

        string prefix = _appConfigurationService.Application.DefaultCommandPrefix;

        if (TryGetGuild(discordId, out var guild))
        {
            prefix = guild.CommandPrefix;
        }

        return prefix;
    }

    public async Task SetNewPrefix(ulong guildId, string newPrefix)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(SetNewPrefix));

        if (TryGetGuild(guildId, out var guild))
        {
            _logger.LogInformation($"Updating prefix for Guild [{guildId}]");
            // Clone user to avoid making change to cache till db change confirmed.
            var copyOfGuild = guild.Clone();
            copyOfGuild.CommandPrefix = newPrefix;

            await UpdateGuild(copyOfGuild);
        }

        transaction.Finish();
    }

    public async Task SetLevellingChannel(ulong guildId, ulong channelId)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(SetLevellingChannel));

        if (TryGetGuild(guildId, out var guild))
        {
            _logger.LogInformation($"Updating Levelling Channel for Guild [{guildId}]");
            // Clone guild to avoid making change to cache till db change confirmed.
            var copyOfGuild = guild.Clone();
            copyOfGuild.LevelAnnouncementChannelId = channelId;

            await UpdateGuild(copyOfGuild);
        }

        transaction.Finish();
    }

    public async Task<bool> ToggleDadJoke(ulong guildId)
    {
        bool currentSet = false;
        if (TryGetGuild(guildId, out var guild))
        {
            var copyOfGuild = guild.Clone();
            copyOfGuild.DadJokesEnabled = !copyOfGuild.DadJokesEnabled;
            currentSet = copyOfGuild.DadJokesEnabled;

            await UpdateGuild(copyOfGuild);
        }

        return currentSet;
    }

    public async Task IncrementGoodVote(ulong guildId)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(IncrementGoodVote));

        if (TryGetGuild(guildId, out var guild))
        {
            _logger.LogInformation($"Incrementing good bot vote for Guild [{guildId}]");
            var copyOfGuild = guild.Clone();
            copyOfGuild.GoodBotVotes++;

            await UpdateGuild(copyOfGuild);
        }

        transaction.Finish();
    }

    public async Task IncrementBadVote(ulong guildId)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(IncrementBadVote));

        if (TryGetGuild(guildId, out var guild))
        {
            _logger.LogInformation($"Incrementing bad bot vote for Guild [{guildId}]");
            var copyOfGuild = guild.Clone();
            copyOfGuild.BadBotVotes++;

            await UpdateGuild(copyOfGuild);
        }

        transaction.Finish();
    }

    public async Task UpsertGuild(Guild guild)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(UpsertGuild));

        if (BotKnowsGuild(guild.DiscordId))
        {
            await UpdateGuild(guild);
        }
        else
        {
            await InsertGuild(guild);
        }

        transaction.Finish();
    }

    public async Task UpdateGuildName(ulong guildId, string newName)
    {
        if (TryGetGuild(guildId, out var guild))
        {
            if (!newName.Equals(guild.Name))
            {
                _logger.LogInformation("The name of Guild {GuildId} has changed from {OldName} to {NewName} and will be updated in the database", guildId, guild.Name, newName);
                guild.Name = newName;
                await UpdateGuild(guild);
            }
        }
        else
        {
            _logger.LogInformation("The Guild {GuildId} does not exist so will be created in the database", guildId);
            await InsertGuild(new Guild(guildId, newName));
        }
    }

    public async Task RemoveGuild(ulong guildId)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(RemoveGuild));

        if (TryGetGuild(guildId, out var guild))
        {
            _logger.LogInformation("Deleting a Guild [{GuildId}] from the database.", guildId);
            int writtenCount;
            using (var db = _dbContextFactory.CreateDbContext())
            {
                db.Guilds.Remove(guild);
                writtenCount = await db.SaveChangesAsync();
            }

            if (writtenCount > 0)
            {
                _guildsByDiscordId.Remove(guild.DiscordId);
            }
            else
            {
                _logger.LogError("Deleting Guild [{GuildId}] from the database altered no entities. The internal cache was not changed.", guildId);
            }
        }

        transaction.Finish();
    }

    private async Task InsertGuild(Guild guild)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(InsertGuild));

        _logger.LogInformation($"Writing a new Guild [{guild.DiscordId}] to the database.");
        int writtenCount;
        using (var db = _dbContextFactory.CreateDbContext())
        {
            db.Guilds.Add(guild);
            writtenCount = await db.SaveChangesAsync();
        }
        if (writtenCount > 0)
        {
            _guildsByDiscordId.Add(guild.DiscordId, guild);
        }
        else
        {
            _logger.LogError($"Writing Guild [{guild.DiscordId}] to the database inserted no entities. The internal cache was not changed.");
        }

        transaction.Finish();
    }

    private async Task UpdateGuild(Guild guild)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(UpdateGuild));

        _logger.LogInformation("Updating the Guild {GuildId} in the database", guild.DiscordId);
        guild.RowId = _guildsByDiscordId[guild.DiscordId].RowId;
        int writtenCount;
        using (var db = _dbContextFactory.CreateDbContext())
        {
            // To avoid EF tracking issue, grab and alter existing entity.
            var original = db.Guilds.First(u => u.RowId == guild.RowId);
            db.Entry(original).CurrentValues.SetValues(guild);
            db.Guilds.Update(original);
            writtenCount = await db.SaveChangesAsync();
        }
        if (writtenCount > 0)
        {
            _guildsByDiscordId[guild.DiscordId] = guild;
        }
        else
        {
            _logger.LogError("Updating Guild {GuildId} in the database altered no entities. The internal cache was not changed", guild.DiscordId);
        }

        transaction.Finish();
    }
}