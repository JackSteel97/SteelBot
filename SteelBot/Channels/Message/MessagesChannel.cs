using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using SteelBot.Channels.Voice;
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
        private readonly VoiceStateChangeHandler _voiceStateChangeHanlder;
        private readonly UserLockingService _userLockingService;
        private readonly Dictionary<(ulong guildId, ulong userId), DateTime> _lastVoiceUpdateFromMessage;
        private static readonly TimeSpan _voiceUpdateTimeout = TimeSpan.FromMinutes(1);

        public MessagesChannel(ILogger<MessagesChannel> logger,
            ErrorHandlingService errorHandlingService,
            UserTrackingService userTrackingService,
            DiscordClient discordClient,
            IncomingMessageHandler incomingMessageHandler,
            VoiceStateChangeHandler voiceStateChangeHanlder,
            UserLockingService userLockingService) : base(logger, errorHandlingService, "Messages")
        {
            _userTrackingService = userTrackingService;
            _discordClient = discordClient;
            _incomingMessageHandler = incomingMessageHandler;
            _voiceStateChangeHanlder = voiceStateChangeHanlder;
            _userLockingService = userLockingService;
        }

        protected override async ValueTask HandleMessage(IncomingMessage message)
        {
            using (await _userLockingService.WriterLockAsync(message.Guild.Id, message.User.Id))
            {
                if (await _userTrackingService.TrackUser(message.Guild.Id, message.User, message.Guild, _discordClient))
                {
                    await _incomingMessageHandler.HandleMessage(message);

                    var key = (message.Guild.Id, message.User.Id);
                    if (!_lastVoiceUpdateFromMessage.TryGetValue(key, out var lastUpdate) || (DateTime.UtcNow - lastUpdate) > _voiceUpdateTimeout)
                    {
                        _lastVoiceUpdateFromMessage[key] = DateTime.UtcNow;
                        // The user here is already coming from a Guild so we can safely cast to a member.
                        var member = (DiscordMember)message.User;
                        await _voiceStateChangeHanlder.HandleVoiceStateChange(new VoiceStateChange(message.Guild, message.User, member.VoiceState));
                    }
                }
            }
        }
    }
}
