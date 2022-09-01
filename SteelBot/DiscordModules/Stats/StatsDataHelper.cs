using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Humanizer;
using Microsoft.Extensions.Logging;
using SteelBot.Database.Models;
using SteelBot.Database.Models.Pets;
using SteelBot.Database.Models.Users;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.DiscordModules.RankRoles;
using SteelBot.DiscordModules.Stats.Models;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Helpers.Levelling;
using SteelBot.Services;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Stats;

public class StatsDataHelper
{
    private readonly DataCache _cache;
    private readonly AppConfigurationService _appConfigurationService;
    private readonly ILogger<StatsDataHelper> _logger;
    private readonly PetsDataHelper _petsDataHelper;

    public StatsDataHelper(DataCache cache,
        AppConfigurationService appConfigurationService,
        ILogger<StatsDataHelper> logger,
        PetsDataHelper petsDataHelper)
    {
        _cache = cache;
        _appConfigurationService = appConfigurationService;
        _logger = logger;
        _petsDataHelper = petsDataHelper;
    }

    public static DiscordEmbedBuilder GetStatsEmbed(User user, string username)
    {
        var embedBuilder = new DiscordEmbedBuilder()
            .WithColor(EmbedGenerator.InfoColour)
            .WithTitle($"{username} Stats")
            .AddField("Message Count", $"`{user.MessageCount:N0} Messages`", true)
            .AddField("Average Message Length", $"`{user.GetAverageMessageLength()} Characters`", true)
            .AddField("AFK Time", $"`{user.TimeSpentAfk.Humanize(2)}`", true)
            .AddField("Voice Time", $"`{user.TimeSpentInVoice.Humanize(2)} (100%)`", true)
            .AddField("Streaming Time", $"`{user.TimeSpentStreaming.Humanize(2)} ({MathsHelper.GetPercentageOfDuration(user.TimeSpentStreaming, user.TimeSpentInVoice):P2})`", true)
            .AddField("Video Time", $"`{user.TimeSpentOnVideo.Humanize(2)} ({MathsHelper.GetPercentageOfDuration(user.TimeSpentOnVideo, user.TimeSpentInVoice):P2})`", true)
            .AddField("Muted Time", $"`{user.TimeSpentMuted.Humanize(2)} ({MathsHelper.GetPercentageOfDuration(user.TimeSpentMuted, user.TimeSpentInVoice):P2})`", true)
            .AddField("Deafened Time", $"`{user.TimeSpentDeafened.Humanize(2)} ({MathsHelper.GetPercentageOfDuration(user.TimeSpentDeafened, user.TimeSpentInVoice):P2})`", true);

        return embedBuilder;
    }

    /// <summary>
    /// Called during app shutdown to make sure no timings get carried too long during downtime.
    /// </summary>
    public async Task DisconnectAllUsers()
    {
        _logger.LogInformation("Disconnecting all users from voice stats");
        var allUsers = _cache.Users.GetAllUsers();

        foreach (var user in allUsers)
        {
            var copyOfUser = user.Clone();
            var availablePets = _petsDataHelper.GetAvailablePets(user.Guild.DiscordId, user.DiscordId, out _);

            // Pass null to reset all start times.
            copyOfUser.VoiceStateChange(newState: null, availablePets, scalingFactor: 1, shouldEarnVideoXp: true, updateLastActivity: false);
            copyOfUser.UpdateLevel();
            await _cache.Users.UpdateUser(user.Guild.DiscordId, copyOfUser);
            await _petsDataHelper.PetXpUpdated(availablePets, default, copyOfUser.CurrentLevel); // Default - Don't try to send level up messages
        }
    }

    public bool TryGetUser(ulong guildId, ulong discordId, out User user) => _cache.Users.TryGetUser(guildId, discordId, out user);

    public List<User> GetUsersInGuild(ulong guildId) => _cache.Users.GetUsersInGuild(guildId);

    public List<CommandStatistic> GetCommandStatistics() => _cache.CommandStatistics.GetAllCommandStatistics();

    
}