using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using SteelBot.Channels.Voice;
using SteelBot.DiscordModules.Config;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SteelBot.Channels.Message
{
    public class MessagesChannel
    {
        private const int MaxCapacity = 10_000;
        private bool _started = false;

        private readonly Channel<IncomingMessage> _channel;
        private readonly ILogger<MessagesChannel> _logger;
        private readonly ErrorHandlingService _errorHandlingService;
        private readonly UserTrackingService _userTrackingService;
        private readonly DiscordClient _discordClient;
        private readonly IncomingMessageHandler _incomingMessageHandler;
        private readonly VoiceStateChangeHandler _voiceStateChangeHanlder;

        public MessagesChannel(ILogger<MessagesChannel> logger,
            ErrorHandlingService errorHandlingService,
            UserTrackingService userTrackingService,
            DiscordClient discordClient,
            IncomingMessageHandler incomingMessageHandler,
            VoiceStateChangeHandler voiceStateChangeHanlder)
        {
            _logger = logger;
            _errorHandlingService = errorHandlingService;
            _userTrackingService = userTrackingService;
            _discordClient = discordClient;
            _incomingMessageHandler = incomingMessageHandler;
            _voiceStateChangeHanlder = voiceStateChangeHanlder;

            var options = new BoundedChannelOptions(MaxCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
            };

            _channel = Channel.CreateBounded<IncomingMessage>(options);
        }

        public async ValueTask WriteMessage(IncomingMessage message, CancellationToken token)
        {
            try
            {
                await _channel.Writer.WriteAsync(message, token);
            }
            catch (ChannelClosedException e)
            {
                _logger.LogWarning("Attempt to write to a closed messages channel could not complete because the channel has been closed: {Exception}", e.ToString());
            }
            catch (OperationCanceledException e)
            {
                _logger.LogWarning("Attempt to write to a messages channel was cancelled with exception {Exception}", e.ToString());
            }
        }

        public void Start(CancellationToken token)
        {
            if (!_started)
            {
                _started = true;
                StartConsumer(token).FireAndForget(_errorHandlingService);
            }
        }

        private async Task StartConsumer(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var message = await _channel.Reader.ReadAsync(token);
                    await HandleMessage(message);
                }
            }
            catch (ChannelClosedException e)
            {
                _logger.LogWarning("Attempt to read from a closed messages channel could not complete because the channel has been closed: {Exception}", e.ToString());
            }
            catch (OperationCanceledException e)
            {
                _logger.LogWarning("Attempt to read from a messages channel was cancelled with exception {Exception}", e.ToString());
            }
        }

        private async ValueTask HandleMessage(IncomingMessage message)
        {
            if (await _userTrackingService.TrackUser(message.Guild.Id, message.User, message.Guild, _discordClient))
            {
                await _incomingMessageHandler.HandleMessage(message);

                // The user here is already coming from a Guild so we can safely cast to a member.
                var member = (DiscordMember)message.User;
                await _voiceStateChangeHanlder.HandleVoiceStateChange(new VoiceStateChange(message.Guild, message.User, member.VoiceState));
            }
        }
    }
}
