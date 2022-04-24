using DSharpPlus;
using Microsoft.Extensions.Logging;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.Channels.Voice
{
    public class VoiceStateChannel : BaseChannel<VoiceStateChange>
    {
        private readonly VoiceStateChangeHandler _voiceStateChangeHandler;
        private readonly UserTrackingService _userTrackingService;
        private readonly DiscordClient _discordClient;
        private readonly UserLockingService _userLockingService;

        public VoiceStateChannel(ILogger<VoiceStateChannel> logger,
            ErrorHandlingService errorHandlingService,
            VoiceStateChangeHandler voiceStateChangeHandler,
            UserTrackingService userTrackingService,
            DiscordClient discordClient,
            UserLockingService userLockingService) : base(logger, errorHandlingService, "Voice State")
        {
            _voiceStateChangeHandler = voiceStateChangeHandler;
            _userTrackingService = userTrackingService;
            _discordClient = discordClient;
            _userLockingService = userLockingService;
        }

        protected override async ValueTask HandleMessage(VoiceStateChange message)
        {
            using (await _userLockingService.WriterLockAsync(message.Guild.Id, message.User.Id))
            {
                if (await _userTrackingService.TrackUser(message.Guild.Id, message.User, message.Guild, _discordClient))
                {
                    await _voiceStateChangeHandler.HandleVoiceStateChange(message);
                }
            }
        }
    }
}
