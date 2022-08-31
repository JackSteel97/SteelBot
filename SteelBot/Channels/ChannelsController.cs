using SteelBot.Channels.Message;
using SteelBot.Channels.Pets;
using SteelBot.Channels.RankRole;
using SteelBot.Channels.SelfRole;
using SteelBot.Channels.Voice;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SteelBot.Channels;

public static class ChannelsController
{
    public static PetCommandsChannel PetCommandsChannel { private get; set; }
    public static MessagesChannel MessagesChannel { private get; set; }
    public static RankRoleManagementChannel RankRoleManagementChannel { private get; set; }
    public static SelfRoleManagementChannel SelfRoleManagementChannel { private get; set; }
    public static VoiceStateChannel VoiceStateChannel { private get; set; }

    public static ValueTask SendMessage(PetCommandAction message, CancellationToken cancellationToken)
    {
        if (PetCommandsChannel == default)
            throw new InvalidOperationException("The Pet channel has not been set yet");
        return PetCommandsChannel.Write(message, cancellationToken);
    }

    public static ValueTask SendMessage(IncomingMessage message, CancellationToken cancellationToken)
    {
        if (MessagesChannel == default)
            throw new InvalidOperationException("The Messages channel has not been set yet");
        return MessagesChannel.Write(message, cancellationToken);
    }

    public static ValueTask SendMessage(RankRoleManagementAction message, CancellationToken cancellationToken)
    {
        if (RankRoleManagementChannel == default)
            throw new InvalidOperationException("The Rank Role channel has not been set yet");
        return RankRoleManagementChannel.Write(message, cancellationToken);
    }

    public static ValueTask SendMessage(SelfRoleManagementAction message, CancellationToken cancellationToken)
    {
        if (SelfRoleManagementChannel == default)
            throw new InvalidOperationException("The Self Role channel has not been set yet");
        return SelfRoleManagementChannel.Write(message, cancellationToken);
    }

    public static ValueTask SendMessage(VoiceStateChange message, CancellationToken cancellationToken)
    {
        if (VoiceStateChannel == default)
            throw new InvalidOperationException("The Voice State channel has not been set yet");
        return VoiceStateChannel.Write(message, cancellationToken);
    }
}