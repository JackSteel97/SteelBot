using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using SteelBot.DiscordModules.AuditLog.Services;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Services.Configuration;
using System;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.NonGroupedCommands;

[Description("Miscellaneous commands.")]
[RequireGuild]
public class MiscCommands : TypingCommandModule
{
    private readonly AppConfigurationService _appConfigurationService;

    public MiscCommands(AppConfigurationService appConfigurationService, ILogger<MiscCommands> logger, AuditLogService auditLogService) : base(logger, auditLogService)
    {
        _appConfigurationService = appConfigurationService;
    }

    [Command("Invite")]
    [Aliases("inv", "add", "join")]
    [Description("Get a link to add the bot to your own server.")]
    [Cooldown(1, 60, CooldownBucketType.Channel)]
    public async Task Invite(CommandContext context)
    {
        var invLink = new Uri(_appConfigurationService.Application.InviteLink);
        var message = new DiscordMessageBuilder()
            .WithReply(context.Message.Id, true)
            .WithEmbed(EmbedGenerator.Info(Formatter.MaskedUrl("Here you go!", invLink, "Click me to invite to your own server.")));
        await context.RespondAsync(message);
    }
}