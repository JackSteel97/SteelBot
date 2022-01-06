using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SteelBot.Database;
using SteelBot.Database.Models;
using SteelBot.Services.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders
{
    public class GuildsProvider
    {
        private readonly ILogger<GuildsProvider> Logger;
        private readonly IDbContextFactory<SteelBotContext> DbContextFactory;
        private readonly AppConfigurationService AppConfigurationService;

        private Dictionary<ulong, Guild> GuildsByDiscordId;

        public GuildsProvider(ILogger<GuildsProvider> logger, IDbContextFactory<SteelBotContext> contextFactory, AppConfigurationService appConfigurationService)
        {
            Logger = logger;
            DbContextFactory = contextFactory;
            AppConfigurationService = appConfigurationService;

            GuildsByDiscordId = new Dictionary<ulong, Guild>();
            LoadGuildData();
        }

        private void LoadGuildData()
        {
            Logger.LogInformation("Loading data from database: Guilds");
            using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                GuildsByDiscordId = db.Guilds.AsNoTracking().ToDictionary(g => g.DiscordId);
            }
        }

        public bool BotKnowsGuild(ulong discordId)
        {
            return GuildsByDiscordId.ContainsKey(discordId);
        }

        public bool TryGetGuild(ulong discordId, out Guild guild)
        {
            return GuildsByDiscordId.TryGetValue(discordId, out guild);
        }

        public string GetGuildPrefix(ulong discordId)
        {
            string prefix = AppConfigurationService.Application.DefaultCommandPrefix;

            if (TryGetGuild(discordId, out Guild guild))
            {
                prefix = guild.CommandPrefix;
            }
            return prefix;
        }

        public async Task SetNewPrefix(ulong guildId, string newPrefix)
        {
            if (TryGetGuild(guildId, out Guild guild))
            {
                Logger.LogInformation($"Updating prefix for Guild [{guildId}]");
                // Clone user to avoid making change to cache till db change confirmed.
                Guild copyOfGuild = guild.Clone();
                copyOfGuild.CommandPrefix = newPrefix;

                await UpdateGuild(copyOfGuild);
            }
        }

        public async Task SetLevellingChannel(ulong guildId, ulong channelId)
        {
            if (TryGetGuild(guildId, out Guild guild))
            {
                Logger.LogInformation($"Updating Levelling Channel for Guild [{guildId}]");
                // Clone guild to avoid making change to cache till db change confirmed.
                Guild copyOfGuild = guild.Clone();
                copyOfGuild.LevelAnnouncementChannelId = channelId;

                await UpdateGuild(copyOfGuild);
            }
        }

        public async Task IncrementGoodVote(ulong guildId)
        {
            if (TryGetGuild(guildId, out Guild guild))
            {
                Logger.LogInformation($"Incrementing good bot vote for Guild [{guildId}]");
                Guild copyOfGuild = guild.Clone();
                copyOfGuild.GoodBotVotes++;

                await UpdateGuild(copyOfGuild);
            }
        }

        public async Task IncrementBadVote(ulong guildId)
        {
            if (TryGetGuild(guildId, out Guild guild))
            {
                Logger.LogInformation($"Incrementing bad bot vote for Guild [{guildId}]");
                Guild copyOfGuild = guild.Clone();
                copyOfGuild.BadBotVotes++;

                await UpdateGuild(copyOfGuild);
            }
        }

        public async Task UpsertGuild(Guild guild)
        {
            if (BotKnowsGuild(guild.DiscordId))
            {
                await UpdateGuild(guild);
            }
            else
            {
                await InsertGuild(guild);
            }
        }

        public async Task RemoveGuild(ulong guildId)
        {
            if (TryGetGuild(guildId, out var guild))
            {
                Logger.LogInformation("Deleting a Guild [{GuildId}] from the database.", guildId);
                int writtenCount;
                using (SteelBotContext db = DbContextFactory.CreateDbContext())
                {
                    db.Guilds.Remove(guild);
                    writtenCount = await db.SaveChangesAsync();
                }

                if (writtenCount > 0)
                {
                    GuildsByDiscordId.Remove(guild.DiscordId);
                }
                else
                {
                    Logger.LogError("Deleting Guild [{GuildId}] from the database altered no entities. The internal cache was not changed.", guildId);
                }
            }
        }

        private async Task InsertGuild(Guild guild)
        {
            Logger.LogInformation($"Writing a new Guild [{guild.DiscordId}] to the database.");
            int writtenCount;
            using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                db.Guilds.Add(guild);
                writtenCount = await db.SaveChangesAsync();
            }
            if (writtenCount > 0)
            {
                GuildsByDiscordId.Add(guild.DiscordId, guild);
            }
            else
            {
                Logger.LogError($"Writing Guild [{guild.DiscordId}] to the database inserted no entities. The internal cache was not changed.");
            }
        }

        

        private async Task UpdateGuild(Guild guild)
        {
            Logger.LogInformation($"Updating the Guild [{guild.DiscordId}] in the database.");
            guild.RowId = GuildsByDiscordId[guild.DiscordId].RowId;
            int writtenCount;
            using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                // To avoid EF tracking issue, grab and alter existing entity.
                Guild original = db.Guilds.First(u => u.RowId == guild.RowId);
                db.Entry(original).CurrentValues.SetValues(guild);
                db.Guilds.Update(original);
                writtenCount = await db.SaveChangesAsync();
            }
            if (writtenCount > 0)
            {
                GuildsByDiscordId[guild.DiscordId] = guild;
            }
            else
            {
                Logger.LogError($"Updating Guild [{guild.DiscordId}] in the database altered no entities. The internal cache was not changed.");
            }
        }
    }
}