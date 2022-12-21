using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using SteelBot.DiscordModules.AuditLog.Services;
using System.Threading.Tasks;

namespace SteelBot.Helpers.Extensions;

public class InstrumentedApplicationCommandModule : ApplicationCommandModule
{
    private readonly ILogger _logger;
    private readonly AuditLogService _auditLogService;

    protected InstrumentedApplicationCommandModule(ILogger logger, AuditLogService auditLogService)
    {
        _logger = logger;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public override async Task<bool> BeforeSlashExecutionAsync(InteractionContext ctx)
    {
        _logger.LogInformation("Starting slash command {Command} invoked by {UserId} in {GuildId}", ctx.QualifiedName, ctx.User.Id, ctx.Guild.Id);
        await _auditLogService.UsedSlashCommand(ctx);
        return await base.BeforeSlashExecutionAsync(ctx);
    }

    /// <inheritdoc />
    public override Task AfterSlashExecutionAsync(InteractionContext ctx)
    {
        _logger.LogInformation("Finished slash command {Command} invoked by {UserId} in {GuildId}", ctx.QualifiedName, ctx.User.Id, ctx.Guild.Id);
        return base.AfterSlashExecutionAsync(ctx);
    }

    /// <inheritdoc />
    public override Task<bool> BeforeContextMenuExecutionAsync(ContextMenuContext ctx)
    {
        _logger.LogInformation("Starting context menu {Command} invoked by {UserId} in {GuildId}", ctx.QualifiedName, ctx.User.Id, ctx.Guild.Id);
        return base.BeforeContextMenuExecutionAsync(ctx);
    }

    /// <inheritdoc />
    public override Task AfterContextMenuExecutionAsync(ContextMenuContext ctx)
    {
        _logger.LogInformation("Finished context menu {Command} invoked by {UserId} in {GuildId}", ctx.QualifiedName, ctx.User.Id, ctx.Guild.Id);
        return base.AfterContextMenuExecutionAsync(ctx);
    }
}