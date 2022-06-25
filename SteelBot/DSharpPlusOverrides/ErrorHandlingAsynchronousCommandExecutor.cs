using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Executors;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteelBot.DSharpPlusOverrides
{
    public sealed class ErrorHandlingAsynchronousCommandExecutor : ICommandExecutor
    {
        private readonly ErrorHandlingService ErrorHandlingService;
        private readonly ILogger<ErrorHandlingAsynchronousCommandExecutor> _logger;
        public ErrorHandlingAsynchronousCommandExecutor(ErrorHandlingService errorHandlingService, ILogger<ErrorHandlingAsynchronousCommandExecutor> logger)
        {
            ErrorHandlingService = errorHandlingService;
            _logger = logger;
        }

        public Task ExecuteAsync(CommandContext ctx)
        {
            _logger.BeginScope(new Dictionary<string, object>
            {
                ["Action"] = ctx.Command.QualifiedName
            });

            // Don't wait for completion but also catch failed tasks.
            ctx.CommandsNext.ExecuteCommandAsync(ctx).FireAndForget(ErrorHandlingService);
            //transaction.Finish();
            return Task.CompletedTask;
        }
        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
