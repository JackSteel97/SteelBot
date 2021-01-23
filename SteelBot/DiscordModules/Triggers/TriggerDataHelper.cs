using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using SteelBot.Database.Models;
using SteelBot.DataProviders;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.RankRoles
{
    public class TriggerDataHelper
    {
        private readonly ILogger<TriggerDataHelper> Logger;
        private readonly DataCache Cache;

        public TriggerDataHelper(DataCache cache, ILogger<TriggerDataHelper> logger)
        {
            Cache = cache;
            Logger = logger;
        }

        public async Task CreateTrigger(ulong guildId, ulong creatorId, Trigger trigger)
        {
            Logger.LogInformation($"Request to create Trigger [{trigger.TriggerText}] in Guild [{guildId}] received");
            if (Cache.Guilds.TryGetGuild(guildId, out Guild guild))
            {
                trigger.GuildRowId = guild.RowId;
            }
            else
            {
                Logger.LogWarning($"Could not create Trigger because Guild [{guildId}] does not exist.");
            }
            if (Cache.Users.TryGetUser(guildId, creatorId, out User user))
            {
                trigger.CreatorRowId = user.RowId;
                await Cache.Triggers.AddTrigger(guildId, trigger, user);
            }
            else
            {
                Logger.LogWarning($"Could not create Trigger because User [{creatorId}] does not exist.");
            }
        }

        public async Task<bool> DeleteTrigger(ulong guildId, string triggerText, DiscordMember deleter, DiscordChannel currentChannel)
        {
            Logger.LogInformation($"Request to delete Trigger [{triggerText}] in Guild [{guildId}] received.");

            if (Cache.Triggers.TryGetTrigger(guildId, triggerText, out Trigger trigger))
            {
                bool isGlobalTrigger = !trigger.ChannelDiscordId.HasValue;
                bool canDelete;
                if (isGlobalTrigger)
                {
                    // Get permissions in current channel.
                    Permissions deleterPerms = deleter.PermissionsIn(currentChannel);
                    canDelete = trigger.Creator.DiscordId == deleter.Id || deleterPerms.HasPermission(Permissions.ManageChannels);
                }
                else
                {
                    // Get permissions is the trigger's channel.
                    Permissions deleterPerms = deleter.PermissionsIn(currentChannel.Guild.GetChannel(trigger.ChannelDiscordId.Value));
                    canDelete = trigger.Creator.DiscordId == deleter.Id || deleterPerms.HasPermission(Permissions.ManageMessages);
                }

                if (canDelete)
                {
                    await Cache.Triggers.RemoveTrigger(guildId, triggerText);
                    return true;
                }
            }
            return false;
        }

        public bool TriggerExists(ulong guildId, string triggerText)
        {
            return Cache.Triggers.BotKnowsTrigger(guildId, triggerText);
        }

        public bool GetGuildTriggers(ulong guildId, out Dictionary<string, Trigger> triggers)
        {
            return Cache.Triggers.TryGetGuildTriggers(guildId, out triggers);
        }

        public async Task HandleNewMessage(ulong guildId, DiscordChannel channelId, string messageContent)
        {
            if (Cache.Triggers.TryGetGuildTriggers(guildId, out Dictionary<string, Trigger> triggers))
            {
                foreach (var trigger in triggers.Values)
                {
                    if (!trigger.ChannelDiscordId.HasValue || trigger.ChannelDiscordId.GetValueOrDefault() == channelId.Id)
                    {
                        // Can activate this trigger in this channel.

                        bool activateTrigger;
                        if (trigger.ExactMatch)
                        {
                            activateTrigger = messageContent.Equals(trigger.TriggerText, StringComparison.OrdinalIgnoreCase);
                        }
                        else
                        {
                            activateTrigger = messageContent.Contains(trigger.TriggerText, StringComparison.OrdinalIgnoreCase);
                        }

                        if (activateTrigger)
                        {
                            await channelId.SendMessageAsync(trigger.Response);
                            await Cache.Triggers.IncrementActivations(guildId, trigger);
                        }
                    }
                }
            }
        }
    }
}