using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Logging;
using SteelBot.DiscordModules.AuditLog.Services;
using System.Threading.Tasks;

namespace SteelBot.Helpers.Extensions;

public class TypingCommandModule : BaseCommandModule
{
    private readonly AuditLogService _auditLogService;
    private readonly ILogger _logger;

    protected TypingCommandModule(ILogger logger, AuditLogService auditLogService)
    {
        _logger = logger;
        _auditLogService = auditLogService;
    }

    public override async Task BeforeExecutionAsync(CommandContext ctx)
    {
        _logger.LogInformation("Starting execution of command {Module}.{Command} invoked by {UserId}", ctx.Command.Module.ModuleType.Name, ctx.Command.Name, ctx.User.Id);
        await _auditLogService.UsedCommand(ctx);
        await ctx.TriggerTypingAsync();
    }

    public override Task AfterExecutionAsync(CommandContext ctx)
    {
        _logger.LogInformation("Finished execution of command {Module}.{Command} invoked by {UserId}", ctx.Command.Module.ModuleType.Name, ctx.Command.Name, ctx.User.Id);
        return Task.CompletedTask;
    }
}