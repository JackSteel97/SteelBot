using DSharpPlus.Entities;
using SteelBot.Channels.Stats;
using SteelBot.Database.Models.Pets;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DiscordModules.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.DiscordModules.Stats.Helpers;
using SteelBot.DiscordModules.Stats.Models;
using SteelBot.Helpers.Levelling;
using SteelBot.Services;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Stats.Services;

public class StatsAdminService
{
    private readonly AppConfigurationService _appConfigurationService;
    private readonly PetsDataHelper _petsDataHelper;
    private readonly UserLockingService _userLockingService;
    private readonly UsersProvider _usersProvider;

    public StatsAdminService(UsersProvider usersProvider, PetsDataHelper petsDataHelper, UserLockingService userLockingService, AppConfigurationService appConfigurationService)
    {
        _usersProvider = usersProvider;
        _petsDataHelper = petsDataHelper;
        _userLockingService = userLockingService;
        _appConfigurationService = appConfigurationService;
    }

    public async Task Velocity(StatsCommandAction request)
    {
        if (request.Action != StatsCommandActionType.ViewVelocity) throw new ArgumentException($"Unexpected action type sent to {nameof(Velocity)}");

        XpVelocity velocity;
        using (await _userLockingService.ReaderLockAsync(request.Guild.Id, request.Target.Id))
        {
            var availablePets = _petsDataHelper.GetAvailablePets(request.Guild.Id, request.Target.Id, out _);
            velocity = GetVelocity(request.Target, availablePets);
        }

        request.Responder.Respond(StatsMessages.StatsVelocity(velocity, request.Target.DisplayName));
    }

    public async Task Breakdown(StatsCommandAction request)
    {
        if (request.Action != StatsCommandActionType.ViewBreakdown) throw new ArgumentException($"Unexpected action type sent to {nameof(Breakdown)}");

        using (await _userLockingService.ReaderLockAsync(request.Guild.Id, request.Target.Id))
        {
            if (!_usersProvider.TryGetUser(request.Guild.Id, request.Target.Id, out var user))
            {
                request.Responder.Respond(StatsMessages.UnableToFindUser());
                return;
            }

            request.Responder.Respond(StatsMessages.StatsBreakdown(user, request.Target.DisplayName));
        }
    }

    private XpVelocity GetVelocity(DiscordMember target, List<Pet> availablePets)
    {
        var velocity = new XpVelocity();
        var baseDuration = TimeSpan.FromMinutes(1);
        var levelConfig = _appConfigurationService.Application.Levelling;

        if (_usersProvider.TryGetUser(target.Guild.Id, target.Id, out var user))
        {
            velocity.Message = LevellingMaths.ApplyPetBonuses(levelConfig.MessageXp, availablePets, BonusType.MessageXp);
            velocity.Voice = LevellingMaths.GetDurationXp(baseDuration, user.TimeSpentInVoice, availablePets, BonusType.VoiceXp, levelConfig.VoiceXpPerMin);
            velocity.Muted = LevellingMaths.GetDurationXp(baseDuration, user.TimeSpentMuted, availablePets, BonusType.MutedPenaltyXp, levelConfig.MutedXpPerMin);
            velocity.Deafened = LevellingMaths.GetDurationXp(baseDuration, user.TimeSpentDeafened, availablePets, BonusType.DeafenedPenaltyXp, levelConfig.DeafenedXpPerMin);
            velocity.Streaming = LevellingMaths.GetDurationXp(baseDuration, user.TimeSpentStreaming, availablePets, BonusType.StreamingXp, levelConfig.StreamingXpPerMin);
            velocity.Video = LevellingMaths.GetDurationXp(baseDuration, user.TimeSpentOnVideo, availablePets, BonusType.VideoXp, levelConfig.VideoXpPerMin);

            double disconnectedXpPerMin = PetShared.GetBonusValue(availablePets, BonusType.OfflineXp);
            velocity.Passive = LevellingMaths.GetDurationXp(baseDuration, TimeSpan.Zero, disconnectedXpPerMin);
        }

        return velocity;
    }
}