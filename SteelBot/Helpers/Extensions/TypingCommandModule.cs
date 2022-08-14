using DSharpPlus.CommandsNext;
using Sentry;
using SteelBot.Helpers.Sentry;
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
        Sentry?.StartNewConfiguredTransaction(ctx.Command.Module.ModuleType.Name, ctx.Command.Name, ctx.User, ctx.Guild);
        ctx.TriggerTypingAsync();
        return Task.CompletedTask;
    }

    public override Task AfterExecutionAsync(CommandContext ctx)
    {
        if (Sentry == null) return Task.CompletedTask;
        if (Sentry.TryGetCurrentTransaction(out var transaction))
        {
            transaction.Finish(SpanStatus.Ok);
        }

        return Task.CompletedTask;
    }
}