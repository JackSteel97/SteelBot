using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Executors;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.DSharpPlusOverrides
{
    public sealed class ErrorHandlingAsynchronousCommandExecutor : ICommandExecutor
    {
        private readonly ErrorHandlingService ErrorHandlingService;
        public ErrorHandlingAsynchronousCommandExecutor(ErrorHandlingService errorHandlingService)
        {
            ErrorHandlingService = errorHandlingService;
        }

        public Task ExecuteAsync(CommandContext ctx)
        {
            // Don't wait for completion but also catch failed tasks.
            ctx.CommandsNext.ExecuteCommandAsync(ctx).FireAndForget(ErrorHandlingService);
            return Task.CompletedTask;
        }
        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
