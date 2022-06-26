using DSharpPlus;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.Helpers.Sentry;
using SteelBot.Services;
using System;
using System.Threading.Tasks;

namespace SteelBot.Channels.Voice
{
    public class VoiceStateChannel : BaseChannel<VoiceStateChange>
    {
        private readonly VoiceStateChangeHandler _voiceStateChangeHandler;
        private readonly UserTrackingService _userTrackingService;
        private readonly DiscordClient _discordClient;
        private readonly UserLockingService _userLockingService;
        private readonly IHub _sentry;

        public VoiceStateChannel(ILogger<VoiceStateChannel> logger,
            ErrorHandlingService errorHandlingService,
            VoiceStateChangeHandler voiceStateChangeHandler,
            UserTrackingService userTrackingService,
            DiscordClient discordClient,
            UserLockingService userLockingService,
            IHub sentry) : base(logger, errorHandlingService, "Voice State")
        {
            _voiceStateChangeHandler = voiceStateChangeHandler;
            _userTrackingService = userTrackingService;
            _discordClient = discordClient;
            _userLockingService = userLockingService;
            _sentry = sentry;
        }

        protected override async ValueTask HandleMessage(VoiceStateChange message)
        {
            var transaction = _sentry.StartTransaction("Voice State", "Handle State Change");
            _sentry.ConfigureScope(scope =>
            {
                scope.User = SentryHelpers.GetSentryUser(message.User, message.Guild);
                scope.Transaction = transaction;
            });

            try
            {
                using (await _userLockingService.WriterLockAsync(message.Guild.Id, message.User.Id))
                {
                    var trackUserSpan = transaction.StartChild("Track User");
                    if (await _userTrackingService.TrackUser(message.Guild.Id, message.User, message.Guild,
                            _discordClient))
                    {
                        var stateChangeSpan = trackUserSpan.StartChild("Handle Voice State Change");
                        await _voiceStateChangeHandler.HandleVoiceStateChange(message, stateChangeSpan);
                        stateChangeSpan.Finish();
                    }

                    trackUserSpan.Finish();
                }

                transaction.Finish(SpanStatus.Ok);
            }
            catch (Exception e)
            {
                const string source = $"{nameof(VoiceStateChannel)}.{nameof(HandleMessage)}";
                await _errorHandlingService.Log(e, source);
            }
        }
    }
}
