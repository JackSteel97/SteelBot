using SteelBot.Channels.Stats;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DiscordModules.Stats.Helpers;
using SteelBot.Helpers;
using SteelBot.Services;
using System;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Stats.Services;

public class StatsCardService
{
    private readonly UserLockingService _userLockingService;
    private readonly UsersProvider _usersProvider;

    public StatsCardService(UserLockingService userLockingService, UsersProvider usersProvider)
    {
        _userLockingService = userLockingService;
        _usersProvider = usersProvider;
    }
    
    public async Task View(StatsCommandAction request)
    {
        if (request.Action != StatsCommandActionType.ViewPersonalStats) throw new ArgumentException($"Unexpected action type sent to {nameof(View)}");

        using (await _userLockingService.ReaderLockAsync(request.Guild.Id, request.Target.Id))
        {
            if (!_usersProvider.TryGetUser(request.Guild.Id, request.Target.Id, out var user))
            {
                request.Responder.Respond(StatsMessages.UnableToFindUser());
                return;
            }

            using (var imageStream = await LevelCardGenerator.GenerateCard(user, request.Target))
            {
                var message = StatsMessages.StatsCard(user, request.Target.Username, imageStream);
                request.Responder.Respond(message);
            }
        }
    }
}