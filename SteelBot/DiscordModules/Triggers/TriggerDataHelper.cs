using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using SteelBot.Database.Models;
using SteelBot.Database.Models.Users;
using SteelBot.DataProviders;
using SteelBot.Helpers.Algorithms;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Triggers;

public class TriggerDataHelper
{
    private readonly ILogger<TriggerDataHelper> _logger;
    private readonly DataCache _cache;
    private readonly Random _random = new Random();

    public TriggerDataHelper(DataCache cache, ILogger<TriggerDataHelper> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task CreateTrigger(ulong guildId, ulong creatorId, Trigger trigger)
    {
        _logger.LogInformation($"Request to create Trigger [{trigger.TriggerText}] in Guild [{guildId}] received");
        if (_cache.Guilds.TryGetGuild(guildId, out var guild))
        {
            trigger.GuildRowId = guild.RowId;
        }
        else
        {
            _logger.LogWarning($"Could not create Trigger because Guild [{guildId}] does not exist.");
        }
        if (_cache.Users.TryGetUser(guildId, creatorId, out var user))
        {
            trigger.CreatorRowId = user.RowId;
            await _cache.Triggers.AddTrigger(guildId, trigger, user);
        }
        else
        {
            _logger.LogWarning($"Could not create Trigger because User [{creatorId}] does not exist.");
        }
    }

    public async Task<bool> DeleteTrigger(ulong guildId, string triggerText, DiscordMember deleter, DiscordChannel currentChannel)
    {
        _logger.LogInformation($"Request to delete Trigger [{triggerText}] in Guild [{guildId}] received.");

        if (_cache.Triggers.TryGetTrigger(guildId, triggerText, out var trigger))
        {
            bool isGlobalTrigger = !trigger.ChannelDiscordId.HasValue;
            bool canDelete;
            if (isGlobalTrigger)
            {
                // Get permissions in current channel.
                var deleterPerms = deleter.PermissionsIn(currentChannel);
                canDelete = trigger.Creator.DiscordId == deleter.Id || deleterPerms.HasPermission(Permissions.ManageChannels);
            }
            else
            {
                // Get permissions is the trigger's channel.
                var deleterPerms = deleter.PermissionsIn(currentChannel.Guild.GetChannel(trigger.ChannelDiscordId.Value));
                canDelete = trigger.Creator.DiscordId == deleter.Id || deleterPerms.HasPermission(Permissions.ManageMessages);
            }

            if (canDelete)
            {
                await _cache.Triggers.RemoveTrigger(guildId, triggerText);
                return true;
            }
        }
        return false;
    }

    public bool TriggerExists(ulong guildId, string triggerText) => _cache.Triggers.BotKnowsTrigger(guildId, triggerText);

    public bool GetGuildTriggers(ulong guildId, out Dictionary<string, Trigger> triggers) => _cache.Triggers.TryGetGuildTriggers(guildId, out triggers);

    public async Task CheckForDadJoke(DiscordChannel channel, string messageContent)
    {
        string jokeResult = DadJokeExtractor.Extract(messageContent);
        if (!string.IsNullOrWhiteSpace(jokeResult))
        {
            string response = $"Hi {Formatter.Italic(jokeResult)}, I'm Dad!";
            if (_random.Next(10) == 1)
            {
                response = Uwuifyer.Uwuify(response);
            }
            await channel.SendMessageAsync(response);
        }
    }

    public async Task HandleNewMessage(ulong guildId, DiscordChannel channel, string messageContent)
    {
        if (_cache.Triggers.TryGetGuildTriggers(guildId, out var triggers))
        {
            foreach (var trigger in triggers.Values)
            {
                if (!trigger.ChannelDiscordId.HasValue || trigger.ChannelDiscordId.GetValueOrDefault() == channel.Id)
                {
                    // Can activate this trigger in this channel.
                    bool activateTrigger = trigger.ExactMatch
                        ? messageContent.Equals(trigger.TriggerText, StringComparison.OrdinalIgnoreCase)
                        : messageContent.Contains(trigger.TriggerText, StringComparison.OrdinalIgnoreCase);
                    if (activateTrigger)
                    {
                        await channel.SendMessageAsync(trigger.Response);
                        await _cache.Triggers.IncrementActivations(guildId, trigger);
                    }
                }
            }
        }

        await CheckForDadJoke(channel, messageContent);
    }
}