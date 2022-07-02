using DSharpPlus.SlashCommands;
using Sentry;
using SteelBot.Helpers.Sentry;
using System.Threading.Tasks;

namespace SteelBot.Helpers.Extensions;

public class InstrumentedApplicationCommandModule : ApplicationCommandModule
{
    protected readonly IHub Sentry;
    private readonly string _module;

    protected InstrumentedApplicationCommandModule(string module, IHub sentry = null)
    {
        _module = module;
        Sentry = sentry;
    }

    /// <inheritdoc />
    public override Task<bool> BeforeSlashExecutionAsync(InteractionContext ctx)
    {
        StartTransaction(_module, ctx);
        return base.BeforeSlashExecutionAsync(ctx);
    }

    /// <inheritdoc />
    public override Task AfterSlashExecutionAsync(InteractionContext ctx)
    {
        StopTransaction();
        return base.AfterSlashExecutionAsync(ctx);
    }

    /// <inheritdoc />
    public override Task<bool> BeforeContextMenuExecutionAsync(ContextMenuContext ctx)
    {
        StartTransaction(_module, ctx);
        return base.BeforeContextMenuExecutionAsync(ctx);
    }

    /// <inheritdoc />
    public override Task AfterContextMenuExecutionAsync(ContextMenuContext ctx)
    {
        StopTransaction();
        return base.AfterContextMenuExecutionAsync(ctx);
    }

    private void StartTransaction(string module, BaseContext context)
    {
        if (Sentry != null)
        {
            var transaction = Sentry.StartTransaction(module, context.CommandName);
            Sentry.ConfigureScope(scope =>
            {
                scope.User = context.GetSentryUser();
                scope.Transaction = transaction;
            });
        }
    }

    private void StopTransaction()
    {
        if (Sentry != null)
        {
            ITransaction transaction = Sentry.GetCurrentTransaction();
            if (transaction != null && !transaction.IsFinished)
            {
                transaction.Finish(SpanStatus.Ok);
            }
        }
    }
}