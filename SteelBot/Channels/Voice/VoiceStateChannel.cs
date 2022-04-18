using DSharpPlus;
using Microsoft.Extensions.Logging;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SteelBot.Channels.Voice
{
    public class VoiceStateChannel
    {
        private const int MaxCapacity = 10_000;

        private readonly Channel<VoiceStateChange> _channel;
        private readonly ILogger<VoiceStateChannel> _logger;
        private readonly ErrorHandlingService _errorHandlingService;
        private readonly VoiceStateChangeHandler _voiceStateChangeHandler;
        private readonly UserTrackingService _userTrackingService;
        private readonly DiscordClient _discordClient;

        public VoiceStateChannel(ILogger<VoiceStateChannel> logger,
            ErrorHandlingService errorHandlingService,
            VoiceStateChangeHandler voiceStateChangeHandler,
            UserTrackingService userTrackingService,
            DiscordClient discordClient)
        {
            _logger = logger;
            _errorHandlingService = errorHandlingService;
            _voiceStateChangeHandler = voiceStateChangeHandler;
            _userTrackingService = userTrackingService;
            _discordClient = discordClient;

            var options = new BoundedChannelOptions(MaxCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
            };

            _channel = Channel.CreateBounded<VoiceStateChange>(options);
        }

        public async ValueTask WriteChange(VoiceStateChange change, CancellationToken token)
        {
            try
            {
                await _channel.Writer.WriteAsync(change, token);
            }
            catch (ChannelClosedException e)
            {
                _logger.LogWarning("Attempt to write to a closed voice state channel could not complete because the channel has been closed: {Exception}", e.ToString());
            }
            catch (OperationCanceledException e)
            {
                _logger.LogWarning("Attempt to write to a voice state channel was cancelled with exception {Exception}", e.ToString());
            }
        }

        public void Start(CancellationToken token)
        {
            StartConsumer(token).FireAndForget(_errorHandlingService);
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
                _logger.LogWarning("Attempt to read from a closed voice state channel could not complete because the channel has been closed: {Exception}", e.ToString());
            }
            catch (OperationCanceledException e)
            {
                _logger.LogWarning("Attempt to read from a voice state channel was cancelled with exception {Exception}", e.ToString());
            }
        }

        private async ValueTask HandleMessage(VoiceStateChange message)
        {
            if(await _userTrackingService.TrackUser(message.Guild.Id, message.User, message.Guild, _discordClient))
            {
                await _voiceStateChangeHandler.HandleVoiceStateChange(message);
            }
        }
    }
}
