using DSharpPlus.CommandsNext;
using Sentry;
using System.Threading.Tasks;

namespace SteelBot.Helpers.Extensions;

public class TypingCommandModule : BaseCommandModule
{
    protected readonly IHub Sentry;

    protected TypingCommandModule(IHub sentry = null)
    {
        Sentry = sentry;
    }

    public override Task BeforeExecutionAsync(CommandContext ctx)
    {
        if (Sentry != null)
        {
            var transaction = Sentry.StartTransaction(ctx.Command.Module.ModuleType.Name, ctx.Command.Name);
            Sentry.ConfigureScope(scope =>
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
        if (Sentry != null)
        {
            ITransaction transaction = null;
            Sentry.ConfigureScope(scope => transaction = scope.Transaction);

            if (transaction != null && !transaction.IsFinished)
            {
                transaction.Finish(SpanStatus.Ok);
            }
        }

        return Task.CompletedTask;
    }
}