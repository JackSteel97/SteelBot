﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SteelBot.Database;
using SteelBot.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders
{
    public class TriggersProvider
    {
        private readonly ILogger<TriggersProvider> Logger;
        private readonly IDbContextFactory<SteelBotContext> DbContextFactory;
        private readonly Dictionary<ulong, Dictionary<string, Trigger>> TriggersByGuild;

        public TriggersProvider(ILogger<TriggersProvider> logger, IDbContextFactory<SteelBotContext> contextFactory)
        {
            Logger = logger;
            DbContextFactory = contextFactory;

            TriggersByGuild = new Dictionary<ulong, Dictionary<string, Trigger>>();
            LoadTriggersData();
        }

        public void LoadTriggersData()
        {
            Logger.LogInformation("Loading data from database: Triggers");
            Trigger[] allTriggers;
            using (var db = DbContextFactory.CreateDbContext())
            {
                allTriggers = db.Triggers.AsNoTracking().Include(t => t.Guild).Include(t => t.Creator).ToArray();
            }
            foreach (Trigger trigger in allTriggers)
            {
                AddTriggerToInternalCache(trigger.Guild.DiscordId, trigger);
            }
        }

        private void AddTriggerToInternalCache(ulong guildId, Trigger trigger)
        {
            if (!TriggersByGuild.TryGetValue(guildId, out Dictionary<string, Trigger> triggers))
            {
                triggers = new Dictionary<string, Trigger>();
                TriggersByGuild.Add(guildId, triggers);
            }
            if (!triggers.ContainsKey(trigger.TriggerText.ToLower()))
            {
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

        public async Task AddTrigger(ulong guildId, Trigger trigger)
        {
            if (!BotKnowsTrigger(guildId, trigger.TriggerText))
            {
                await InsertTrigger(guildId, trigger);
            }
        }

        public async Task RemoveTrigger(ulong guildId, string triggerText)
        {
            if (TryGetTrigger(guildId, triggerText, out Trigger trigger))
            {
                await DeleteTrigger(guildId, trigger);
            }
        }

        private async Task InsertTrigger(ulong guildId, Trigger trigger)
        {
            Logger.LogInformation($"Writing a new Trigger [{trigger.TriggerText}] for Guild [{guildId}] to the database.");
            int writtenCount;
            using (var db = DbContextFactory.CreateDbContext())
            {
                db.Triggers.Add(trigger);
                writtenCount = await db.SaveChangesAsync();
            }

            if (writtenCount > 0)
            {
                AddTriggerToInternalCache(guildId, trigger);
            }
            else
            {
                Logger.LogError($"Writing Trigger [{trigger.TriggerText}] for Guild [{guildId}] to the database inserted no entities. The internal cache was not changed.");
            }
        }

        private async Task DeleteTrigger(ulong guildId, Trigger trigger)
        {
            Logger.LogInformation($"Deleting Trigger [{trigger.TriggerText}] for Guild [{guildId}] from the database.");

            int writtenCount;
            using (var db = DbContextFactory.CreateDbContext())
            {
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