using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.Database;
using SteelBot.Database.Models;
using SteelBot.Helpers.Sentry;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using User = SteelBot.Database.Models.Users.User;

namespace SteelBot.DataProviders.SubProviders
{
    public class TriggersProvider
    {
        private readonly ILogger<TriggersProvider> Logger;
        private readonly IDbContextFactory<SteelBotContext> DbContextFactory;
        private readonly Dictionary<ulong, Dictionary<string, Trigger>> TriggersByGuild;
        private readonly IHub _sentry;

        public TriggersProvider(ILogger<TriggersProvider> logger, IDbContextFactory<SteelBotContext> contextFactory, IHub sentry)
        {
            Logger = logger;
            DbContextFactory = contextFactory;
            _sentry = sentry;

            TriggersByGuild = new Dictionary<ulong, Dictionary<string, Trigger>>();
            LoadTriggersData();
        }

        public void LoadTriggersData()
        {
            var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(LoadTriggersData));

            Logger.LogInformation("Loading data from database: Triggers");
            Trigger[] allTriggers;
            using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                allTriggers = db.Triggers.AsNoTracking().Include(t => t.Guild).Include(t => t.Creator).ToArray();
            }
            foreach (Trigger trigger in allTriggers)
            {
                AddTriggerToInternalCache(trigger.Guild.DiscordId, trigger);
            }

            transaction.Finish();
        }

        private void AddTriggerToInternalCache(ulong guildId, Trigger trigger, User creator = null)
        {
            if (!TriggersByGuild.TryGetValue(guildId, out Dictionary<string, Trigger> triggers))
            {
                triggers = new Dictionary<string, Trigger>();
                TriggersByGuild.Add(guildId, triggers);
            }
            if (!triggers.ContainsKey(trigger.TriggerText.ToLower()))
            {
                if (creator != null)
                {
                    trigger.Creator = creator;
                }
                triggers.Add(trigger.TriggerText.ToLower(), trigger);
            }
        }

        private void RemoveTriggerFromInternalCache(ulong guildId, string trigger)
        {
            if (TriggersByGuild.TryGetValue(guildId, out Dictionary<string, Trigger> triggers))
            {
                string key = trigger.ToLower();
                if (triggers.ContainsKey(key))
                {
                    triggers.Remove(key);
                }
            }
        }

        public bool BotKnowsTrigger(ulong guildId, string trigger)
        {
            if (TriggersByGuild.TryGetValue(guildId, out Dictionary<string, Trigger> roles))
            {
                return roles.ContainsKey(trigger.ToLower());
            }
            return false;
        }

        public bool TryGetTrigger(ulong guildId, string triggerText, out Trigger trigger)
        {
            if (TriggersByGuild.TryGetValue(guildId, out Dictionary<string, Trigger> triggers))
            {
                return triggers.TryGetValue(triggerText.ToLower(), out trigger);
            }
            trigger = null;
            return false;
        }

        public bool TryGetGuildTriggers(ulong guildId, out Dictionary<string, Trigger> triggers)
        {
            return TriggersByGuild.TryGetValue(guildId, out triggers);
        }

        public async Task AddTrigger(ulong guildId, Trigger trigger, User creator)
        {
            if (!BotKnowsTrigger(guildId, trigger.TriggerText))
            {
                await InsertTrigger(guildId, trigger, creator);
            }
        }

        public async Task RemoveTrigger(ulong guildId, string triggerText)
        {
            if (TryGetTrigger(guildId, triggerText, out Trigger trigger))
            {
                await DeleteTrigger(guildId, trigger);
            }
        }

        public async Task IncrementActivations(ulong guildId, Trigger trigger)
        {
            trigger.TimesActivated++;
            await UpdateTrigger(guildId, trigger);
        }

        private async Task InsertTrigger(ulong guildId, Trigger trigger, User creator)
        {
            Logger.LogInformation($"Writing a new Trigger [{trigger.TriggerText}] for Guild [{guildId}] to the database.");
            int writtenCount;
            using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                db.Triggers.Add(trigger);
                writtenCount = await db.SaveChangesAsync();
            }

            if (writtenCount > 0)
            {
                AddTriggerToInternalCache(guildId, trigger, creator);
            }
            else
            {
                Logger.LogError($"Writing Trigger [{trigger.TriggerText}] for Guild [{guildId}] to the database inserted no entities. The internal cache was not changed.");
            }
        }

        private async Task UpdateTrigger(ulong guildId, Trigger newTrigger)
        {
            Logger.LogInformation($"Updating Trigger [{newTrigger.TriggerText}] for Guild [{guildId}] in the database.");

            int writtenCount;
            using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                // To prevent EF tracking issue, grab and alter existing value.
                Trigger original = db.Triggers.First(u => u.RowId == newTrigger.RowId);
                db.Entry(original).CurrentValues.SetValues(newTrigger);
                db.Triggers.Update(original);
                writtenCount = await db.SaveChangesAsync();
            }

            if (writtenCount > 0)
            {
                TriggersByGuild[guildId][newTrigger.TriggerText.ToLower()] = newTrigger;
            }
            else
            {
                Logger.LogError($"Updating Trigger [{newTrigger.TriggerText}] in Guild [{guildId}] did not alter any entities. The internal cache was not changed.");
            }
        }

        private async Task DeleteTrigger(ulong guildId, Trigger trigger)
        {
            Logger.LogInformation($"Deleting Trigger [{trigger.TriggerText}] for Guild [{guildId}] from the database.");

            int writtenCount;
            using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                // Remove creator to prevent EF trying to deleting things.
                trigger.Creator = null;
                db.Triggers.Remove(trigger);
                writtenCount = await db.SaveChangesAsync();
            }

            if (writtenCount > 0)
            {
                RemoveTriggerFromInternalCache(guildId, trigger.TriggerText);
            }
            else
            {
                Logger.LogWarning($"Deleting Trigger [{trigger.TriggerText}] for Guild [{guildId}] from the database deleted no entities. The internal cache was not changed.");
            }
        }
    }
}