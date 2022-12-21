﻿using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.Logging;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DiscordModules.AuditLog.Services;
using SteelBot.Helpers.Extensions;
using SteelBot.Responders;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.AuditLog;

[SlashCommandGroup("AuditLog", "Commands for interacting with the audit log")]
[SlashRequireGuild]
public class AuditLogSlashCommands : InstrumentedApplicationCommandModule
{
    private readonly AuditLogProvider _auditLogProvider;
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly ILogger<AuditLogSlashCommands> _logger;

    /// <inheritdoc />
    public AuditLogSlashCommands(AuditLogService auditLogService, AuditLogProvider auditLogProvider, ILogger<AuditLogSlashCommands> logger, ErrorHandlingService errorHandlingService) : base(logger,
        auditLogService)
    {
        _auditLogProvider = auditLogProvider;
        _logger = logger;
        _errorHandlingService = errorHandlingService;
    }

    [SlashCommand("ViewLatest", "View the latest 50 entries in the audit log")]
    [SlashCooldown(1, 60, SlashCooldownBucketType.Guild)]
    public async Task ViewLatest(InteractionContext context)
    {
        var responder = new InteractionResponder(context, _errorHandlingService);
        var audits = await _auditLogProvider.GetLatest(context.Guild.Id);
        var pages = AuditLogViewingService.BuildViewResponsePages(context.Guild, audits);

        responder.RespondPaginated(pages);
    }
}