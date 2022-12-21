using System;

namespace SteelBot.Exceptions;

public class FireAndForgetTaskException : Exception
{
    public FireAndForgetTaskException()
    {
    }

    public FireAndForgetTaskException(string message) : base(message)
    {
    }

    public FireAndForgetTaskException(string message, Exception innerException) : base(message, innerException)
    {
    }
}