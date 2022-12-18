using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.DiscordModules.AuditLog.Services;
using SteelBot.Helpers.Sentry;
using System.Threading.Tasks;

namespace SteelBot.Helpers.Extensions;

public class TypingCommandModule : BaseCommandModule
{
    protected readonly IHub Sentry;
    private readonly ILogger _logger;
    private readonly AuditLogService _auditLogService;

    protected TypingCommandModule(ILogger logger, AuditLogService auditLogService, IHub sentry = null)
    {
        _logger = logger;
        _auditLogService = auditLogService;
        Sentry = sentry;
    }

    public override async Task BeforeExecutionAsync(CommandContext ctx)
    {
        _logger.LogInformation("Starting execution of command {Module}.{Command} invoked by {UserId}", ctx.Command.Module.ModuleType.Name, ctx.Command.Name, ctx.User.Id);
        await _auditLogService.UsedCommand(ctx);
        Sentry?.StartNewConfiguredTransaction(ctx.Command.Module.ModuleType.Name, ctx.Command.Name, ctx.User, ctx.Guild);
        await ctx.TriggerTypingAsync();
    }

    public override Task AfterExecutionAsync(CommandContext ctx)
    {
        _logger.LogInformation("Finished execution of command {Module}.{Command} invoked by {UserId}", ctx.Command.Module.ModuleType.Name, ctx.Command.Name, ctx.User.Id);
        if (Sentry != null && Sentry.TryGetCurrentTransaction(out var transaction))
        {
            transaction.Finish(SpanStatus.Ok);
        }

        return Task.CompletedTask;
    }
}