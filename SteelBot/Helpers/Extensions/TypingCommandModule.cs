using DSharpPlus.CommandsNext;
using Sentry;
using System.Threading.Tasks;

namespace SteelBot.Helpers.Extensions
{
    public class TypingCommandModule : BaseCommandModule
    {
        protected readonly IHub _sentry;

        protected TypingCommandModule(IHub sentry = null)
        {
            _sentry = sentry;
        }

        public override Task BeforeExecutionAsync(CommandContext ctx)
        {
            if (_sentry != null)
            {
                var transaction = _sentry.StartTransaction(ctx.Command.Module.ModuleType.Name, ctx.Command.Name);
                _sentry.ConfigureScope(scope =>
                {
                    scope.User = ctx.GetSentryUser();
                    scope.Transaction = transaction;
                });
            }
            ctx.TriggerTypingAsync();
            return Task.CompletedTask;
        }

        public override Task AfterExecutionAsync(CommandContext ctx)
        {
            if (_sentry != null)
            {
                var transaction = _sentry.GetSpan();
                if (transaction != null && !transaction.IsFinished)
                {
                    transaction.Finish(SpanStatus.Ok);
                }
            }

            return Task.CompletedTask;
        }
    }
}