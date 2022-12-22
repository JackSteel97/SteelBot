namespace SteelBot.Database.Models.AuditLog;

public enum AuditAction
{
    SentMessage,
    JoinedVoiceChannel,
    LeftVoiceChannel,
    JoinedGuild,
    LeftGuild,
    UsedCommand,
    UsedSlashCommand,
    ModalSubmitted,
    MessageReactionAdded
}