using Microsoft.Extensions.Logging;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DiscordModules.AuditLog.Services;
using SteelBot.DiscordModules.Pets;
using SteelBot.DiscordModules.RankRoles.Helpers;
using SteelBot.DiscordModules.Triggers;
using SteelBot.Helpers.Levelling;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.Channels.Message;

public class IncomingMessageHandler
{
    private readonly AuditLogService _auditLogService;
    private readonly LevelMessageSender _levelMessageSender;
    private readonly ILogger<IncomingMessageHandler> _logger;
    private readonly PetsDataHelper _petsDataHelper;
    private readonly RankRolesProvider _rankRolesProvider;
    private readonly TriggerDataHelper _triggerDataHelper;
    private readonly UsersProvider _usersProvider;

    public IncomingMessageHandler(UsersProvider usersProvider,
        ILogger<IncomingMessageHandler> logger,
        LevelMessageSender levelMessageSender,
        PetsDataHelper petsDataHelper,
        TriggerDataHelper triggerDataHelper,
        RankRolesProvider rankRolesProvider,
        AuditLogService auditLogService)
    {
        _usersProvider = usersProvider;
        _logger = logger;
        _levelMessageSender = levelMessageSender;
        _petsDataHelper = petsDataHelper;
        _triggerDataHelper = triggerDataHelper;
        _rankRolesProvider = rankRolesProvider;
        _auditLogService = auditLogService;
    }

    public async Task HandleMessage(IncomingMessage messageArgs)
    {
        await _auditLogService.MessageSent(messageArgs);
        bool levelledUp = await UpdateMessageCounters(messageArgs);

        if (levelledUp) await RankRoleShared.UserLevelledUp(messageArgs.Guild.Id, messageArgs.User.Id, messageArgs.Guild, _rankRolesProvider, _usersProvider, _levelMessageSender);

        await _triggerDataHelper.HandleNewMessage(messageArgs.Guild.Id, messageArgs.Message.Channel, messageArgs.Message.Content);
    }

    private async ValueTask<bool> UpdateMessageCounters(IncomingMessage messageArgs)
    {
        bool levelIncreased = false;
        if (_usersProvider.TryGetUser(messageArgs.Guild.Id, messageArgs.User.Id, out var user))
        {
            _logger.LogInformation("Updating message counters for User {UserId} in Guild {GuildId}", messageArgs.User.Id, messageArgs.Guild.Id);

            // Clone user to avoid making change to cache till db change confirmed.
            var copyOfUser = user.Clone();
            var availablePets = _petsDataHelper.GetAvailablePets(messageArgs.Guild.Id, messageArgs.User.Id, out _);

            if (copyOfUser.NewMessage(messageArgs.Message.Content.Length, availablePets))
            {
                // Xp has changed.
                levelIncreased = copyOfUser.UpdateLevel();

                await _petsDataHelper.PetXpUpdated(availablePets, messageArgs.Guild, copyOfUser.CurrentLevel);
            }

            if (user.ConsecutiveDaysActive != copyOfUser.ConsecutiveDaysActive)
            {
                // Streak changed.
                ulong xpEarned = copyOfUser.UpdateStreakXp();
                if (xpEarned > 0) _levelMessageSender.SendStreakMessage(messageArgs.Guild, messageArgs.User, copyOfUser.ConsecutiveDaysActive, xpEarned);
            }

            await _usersProvider.UpdateUser(messageArgs.Guild.Id, copyOfUser);

            if (levelIncreased) _levelMessageSender.SendLevelUpMessage(messageArgs.Guild, messageArgs.User);
        }

        return levelIncreased;
    }
}