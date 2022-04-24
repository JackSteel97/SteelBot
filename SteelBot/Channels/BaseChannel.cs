using Microsoft.Extensions.Logging;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SteelBot.Channels
{
    public abstract class BaseChannel<TMsg>
    {
        private const int MaxCapacity = 10_000;
        protected bool _started = false;

        private readonly Channel<TMsg> _channel;
        private readonly ILogger _logger;
        private readonly ErrorHandlingService _errorHandlingService;
        private readonly string _label;

        protected BaseChannel(ILogger logger, ErrorHandlingService errorHandlingService, string channelLabel = "Unlabelled")
        {
            _logger = logger;
            _errorHandlingService = errorHandlingService;
            _label = channelLabel;

            var options = new BoundedChannelOptions(MaxCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
            };

            _channel = Channel.CreateBounded<TMsg>(options);
        }

        public async ValueTask WriteChange(TMsg change, CancellationToken token)
        {
            try
            {
                await _channel.Writer.WriteAsync(change, token);
            }
            catch (ChannelClosedException e)
            {
                _logger.LogWarning("Attempt to write to a closed {ChannelLabel} channel could not complete because the channel has been closed: {Exception}", _label, e.ToString());
            }
            catch (OperationCanceledException e)
            {
                _logger.LogWarning("Attempt to write to a {ChannelLabel} channel was cancelled with exception {Exception}", _label, e.ToString());
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
                _logger.LogWarning("Attempt to read from a closed {ChannelLabel} channel could not complete because the channel has been closed: {Exception}", _label, e.ToString());
            }
            catch (OperationCanceledException e)
            {
                _logger.LogWarning("Attempt to read from a {ChannelLabel} channel was cancelled with exception {Exception}", _label, e.ToString());
            }
        }

        protected abstract ValueTask HandleMessage(TMsg message);
    }
}
