using Humanizer;
using System;

namespace SteelBot.Exceptions;

public class CommandRateLimitedException : Exception
{
    /// <inheritdoc />
    public override string Message { get; }

    public CommandRateLimitedException() : base()
    {
    }

    public CommandRateLimitedException(string command, int maxUses, TimeSpan inPeriod, TimeSpan remainingCooldown)
    {
        Message = $"{command} can only be used {maxUses} times in {inPeriod.Humanize()}. Please try again in {remainingCooldown.Humanize()}";
    }
}