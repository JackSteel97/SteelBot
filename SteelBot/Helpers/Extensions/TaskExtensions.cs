using SteelBot.Exceptions;
using SteelBot.Services;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SteelBot.Helpers.Extensions;

public static class TaskExtensions
{
    /// <summary>
    ///     Handle any unobserved exceptions raised by an un-awaited task.
    /// </summary>
    /// <typeparam name="T">Return type of the task.</typeparam>
    /// <param name="task">The task to handle.</param>
    /// <param name="errorHandler">Error handling class</param>
    /// <param name="memberName">The caller method name.</param>
    /// <param name="lineNumber">The line number of the call.</param>
    public static void FireAndForget<T>(this Task<T> task, ErrorHandlingService errorHandler, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0) =>
        task.ContinueWith(async t => await Handle(errorHandler, t.Exception, memberName, lineNumber), TaskContinuationOptions.OnlyOnFaulted);

    /// <summary>
    ///     Handle any unobserved exceptions raised by an un-awaited task.
    /// </summary>
    /// <param name="task">The task to handle.</param>
    /// <param name="errorHandler">Error handling class</param>
    /// <param name="memberName">The caller method name.</param>
    /// <param name="lineNumber">The line number of the call.</param>
    public static void FireAndForget(this Task task, ErrorHandlingService errorHandler, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0) =>
        task.ContinueWith(async t => await Handle(errorHandler, t.Exception, memberName, lineNumber), TaskContinuationOptions.OnlyOnFaulted);

    private static async Task Handle(ErrorHandlingService errorHandler, AggregateException exception, string memberName, int lineNumber)
    {
        var aggException = exception.Flatten();
        foreach (var ex in aggException.InnerExceptions)
        {
            var error = new FireAndForgetTaskException($"An exception occurred within a fire and forget task with the message: {ex.Message}", ex);
            await errorHandler.Log(error, $"{memberName} at Line {lineNumber}");
        }
    }
}