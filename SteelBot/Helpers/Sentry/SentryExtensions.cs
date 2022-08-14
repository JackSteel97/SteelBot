using DSharpPlus.Entities;
using Sentry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        var transaction = GetCurrentTransactionCore(sentry);
        Debug.Assert(transaction != null, "Transaction has not been configured. Configure a transaction at the start of this flow.");
        return transaction;
    }

    public static bool TryGetCurrentTransaction(this IHub sentry, out ITransaction transaction)
    {
        transaction = GetCurrentTransactionCore(sentry);
        return transaction != null;
    }

    private static ITransaction GetCurrentTransactionCore(IHub sentry)
    {
        ITransaction transaction = null;
        sentry.ConfigureScope(scope => transaction = scope.Transaction);
        return transaction;
    }

    public static ISpan StartSpanOnCurrentTransaction(this IHub sentry, string operation, string description = null)
    {
        var transaction = GetCurrentTransaction(sentry);
        return transaction.StartChild(operation, description);
    }
}