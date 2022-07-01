using DSharpPlus.Entities;
using Sentry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.Helpers.Sentry;
public static class SentryExtensions
{
    public static ITransaction StartNewConfiguredTransaction(this IHub sentry, string name, string operation, DiscordUser user,
        DiscordGuild guild)
    {
        // Stop any existing transaction.
        var existingTransaction = sentry.GetSpan();
        if (existingTransaction != null)
        {
            existingTransaction.Finish();
        }

        var transaction = sentry.StartTransaction(name, operation);
        sentry.ConfigureScope(scope =>
        {
            scope.User = SentryHelpers.GetSentryUser(user, guild);
            scope.Transaction = transaction;
        });
        return transaction;
    }

    public static ITransaction StartNewConfiguredTransaction(this IHub sentry, string name, string operation)
    {
        // Stop any existing transaction.
        var existingTransaction = sentry.GetSpan();
        if (existingTransaction != null)
        {
            existingTransaction.Finish();
        }

        var transaction = sentry.StartTransaction(name, operation);
        sentry.ConfigureScope(scope => scope.Transaction = transaction);
        return transaction;
    }

    public static ITransaction GetCurrentTransaction(this IHub sentry)
    {
        ITransaction transaction = null;
        sentry.ConfigureScope(scope =>
        {
            transaction = scope.Transaction;
        });
        return transaction;
    }
}
