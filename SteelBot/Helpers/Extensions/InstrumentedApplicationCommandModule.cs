using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.Helpers.Sentry;
using System.Threading.Tasks;

namespace SteelBot.Helpers.Extensions;

public class InstrumentedApplicationCommandModule : ApplicationCommandModule
{
    protected readonly IHub Sentry;
    private readonly string _module;
    private readonly ILogger _logger;

    protected InstrumentedApplicationCommandModule(string module, ILogger logger, IHub sentry = null)
    {
        _module = module;
        _logger = logger;
        Sentry = sentry;
    }

    /// <inheritdoc />
    public override Task<bool> BeforeSlashExecutionAsync(InteractionContext ctx)
    {
        _logger.LogInformation("Starting slash command {Command} invoked by {UserId} in {GuildId}", ctx.CommandName, ctx.User.Id, ctx.Guild.Id);
        StartTransaction(_module, ctx);
        return base.BeforeSlashExecutionAsync(ctx);
    }

    /// <inheritdoc />
    public override Task AfterSlashExecutionAsync(InteractionContext ctx)
    {
        _logger.LogInformation("Finished slash command {Command} invoked by {UserId} in {GuildId}", ctx.CommandName, ctx.User.Id, ctx.Guild.Id);
        StopTransaction();
        return base.AfterSlashExecutionAsync(ctx);
    }

    /// <inheritdoc />
    public override Task<bool> BeforeContextMenuExecutionAsync(ContextMenuContext ctx)
    {
        _logger.LogInformation("Starting context menu {Command} invoked by {UserId} in {GuildId}", ctx.CommandName, ctx.User.Id, ctx.Guild.Id);
        StartTransaction(_module, ctx);
        return base.BeforeContextMenuExecutionAsync(ctx);
    }

    /// <inheritdoc />
    public override Task AfterContextMenuExecutionAsync(ContextMenuContext ctx)
    {
        _logger.LogInformation("Finished context menu {Command} invoked by {UserId} in {GuildId}", ctx.CommandName, ctx.User.Id, ctx.Guild.Id);
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