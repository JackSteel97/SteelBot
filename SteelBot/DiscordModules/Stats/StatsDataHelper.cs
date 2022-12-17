using Microsoft.Extensions.Logging;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets;
using SteelBot.Helpers.Levelling;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Stats;

public class StatsDataHelper
{
    private readonly DataCache _cache;
    private readonly ILogger<StatsDataHelper> _logger;
    private readonly PetsDataHelper _petsDataHelper;

    public StatsDataHelper(DataCache cache,
        ILogger<StatsDataHelper> logger,
        PetsDataHelper petsDataHelper)
    {
        _cache = cache;
        _logger = logger;
        _petsDataHelper = petsDataHelper;
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
}