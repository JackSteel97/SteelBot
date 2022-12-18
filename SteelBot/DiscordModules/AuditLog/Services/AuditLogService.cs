using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using SteelBot.Channels.Message;
using SteelBot.Channels.Voice;
using SteelBot.Database.Models.AuditLog;
using SteelBot.DataProviders.SubProviders;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.AuditLog.Services;

public class AuditLogService
{
    private readonly ILogger<AuditLogService> _logger;
    private readonly AuditLogProvider _auditLogProvider;

    public AuditLogService(ILogger<AuditLogService> logger, AuditLogProvider auditLogProvider)
    {
        _logger = logger;
        _auditLogProvider = auditLogProvider;
    }

    private Task Log(Audit auditLogEntry)
    {
        return _auditLogProvider.Write(auditLogEntry);
    }

    public Task MessageSent(IncomingMessage message)
    {
        var entry = new Audit(message.User.Id, message.User.Username, AuditAction.SentMessage, message.Guild.Id, message.Guild.Name, message.Message.ChannelId, message.Message.Channel.Name);
        return Log(entry);
    }

    public async Task VoiceStateChange(VoiceStateChange change)
    {
        if (change.After != null && change.Before != null && change.After.Channel != null && change.Before.Channel != null)
        {
            await LeftVoiceChannel(change);
            await JoinVoiceChannel(change);
        }else if (change.After != null && change.After.Channel != null)
        {
            await JoinVoiceChannel(change);
        }else if (change.Before != null && change.Before.Channel != null)
        {
            await LeftVoiceChannel(change);
        }
    }

    public Task JoinedGuild(GuildMemberAddEventArgs args)
    {
        var entry = new Audit(args.Member.Id, args.Member.Username, AuditAction.JoinedGuild, args.Guild.Id, args.Guild.Name);
        return Log(entry);
    }

    public Task LeftGuild(GuildMemberRemoveEventArgs args)
    {
        var entry = new Audit(args.Member.Id, args.Member.Username, AuditAction.LeftGuild, args.Guild.Id, args.Guild.Name);
        return Log(entry);
    }

    public Task UsedCommand(CommandContext context)
    {
        var entry = new Audit(context.User.Id, context.User.Username, AuditAction.UsedCommand, context.Guild.Id, context.Guild.Name, context.Channel.Id, context.Channel.Name);
        entry.Description = $"{context.Command.Module.ModuleType.Name} {context.Command.Name}";
        return Log(entry);
    }

    public Task UsedSlashCommand(InteractionContext context)
    {
        var entry = new Audit(context.User.Id, context.User.Username, AuditAction.UsedSlashCommand, context.Guild.Id, context.Guild.Name, context.Channel.Id, context.Channel.Name);
        entry.Description = context.QualifiedName;
        return Log(entry);
    }

    private Task JoinVoiceChannel(VoiceStateChange change)
    {
        var entry = new Audit(change.User.Id, change.User.Username, AuditAction.JoinedVoiceChannel, change.Guild.Id, change.Guild.Name, change.After.Channel.Id, change.After.Channel.Name);
        return Log(entry);
    }
    
    private Task LeftVoiceChannel(VoiceStateChange change)
    {
        var entry = new Audit(change.User.Id, change.User.Username, AuditAction.LeftVoiceChannel, change.Guild.Id, change.Guild.Name, change.Before.Channel.Id, change.Before.Channel.Name);
        return Log(entry);
    }
}