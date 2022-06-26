using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.Channels.Voice;
using SteelBot.Helpers.Sentry;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteelBot.Channels.Message
{
    public class MessagesChannel : BaseChannel<IncomingMessage>
    {
        private readonly UserTrackingService _userTrackingService;
        private readonly DiscordClient _discordClient;
        private readonly IncomingMessageHandler _incomingMessageHandler;
        private readonly VoiceStateChangeHandler _voiceStateChangeHandler;
        private readonly UserLockingService _userLockingService;
        private readonly IHub _sentry;
        private readonly Dictionary<(ulong guildId, ulong userId), DateTime> _lastVoiceUpdateFromMessage;
        private static readonly TimeSpan _voiceUpdateTimeout = TimeSpan.FromMinutes(1);

        public MessagesChannel(ILogger<MessagesChannel> logger,
            ErrorHandlingService errorHandlingService,
            UserTrackingService userTrackingService,
            DiscordClient discordClient,
            IncomingMessageHandler incomingMessageHandler,
            VoiceStateChangeHandler voiceStateChangeHandler,
            UserLockingService userLockingService,
            IHub sentry) : base(logger, errorHandlingService, "Messages")
        {
            _userTrackingService = userTrackingService;
            _discordClient = discordClient;
            _incomingMessageHandler = incomingMessageHandler;
            _voiceStateChangeHandler = voiceStateChangeHandler;
            _userLockingService = userLockingService;
            _sentry = sentry;
            _lastVoiceUpdateFromMessage = new Dictionary<(ulong guildId, ulong userId), DateTime>();
        }

        protected override async ValueTask HandleMessage(IncomingMessage message)
        {
            var transaction = _sentry.StartTransaction("Messages", "Handle Message");
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
                        var messageSpan = trackUserSpan.StartChild("Handle Message Updates");
                        await _incomingMessageHandler.HandleMessage(message, messageSpan);
                        messageSpan.Finish();

                        var key = (message.Guild.Id, message.User.Id);
                        if (!_lastVoiceUpdateFromMessage.TryGetValue(key, out var lastUpdate) ||
                            (DateTime.UtcNow - lastUpdate) > _voiceUpdateTimeout)
                        {
                            var voiceSpan = trackUserSpan.StartChild("Update Voice States");
                            _lastVoiceUpdateFromMessage[key] = DateTime.UtcNow;

                            // The user here is already coming from a Guild so we can safely cast to a member.
                            var member = (DiscordMember)message.User;
                            await _voiceStateChangeHandler.HandleVoiceStateChange(
                                new VoiceStateChange(message.Guild, message.User, member.VoiceState), voiceSpan);
                            voiceSpan.Finish();
                        }
                    }

                    trackUserSpan.Finish();
                }

                transaction.Finish(SpanStatus.Ok);
            }
            catch (Exception e)
            {
                const string source = $"{nameof(MessagesChannel)}.{nameof(HandleMessage)}";
                await _errorHandlingService.Log(e, source);
            }
        }
    }
}
