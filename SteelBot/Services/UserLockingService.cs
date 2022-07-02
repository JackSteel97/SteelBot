using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SteelBot.Services;

/// <summary>
/// Manages locks on entire users that are used to ensure pipelines of modifications to users are executed in serial and avoid race-conditions.
/// </summary>
public class UserLockingService
{
    private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, AsyncReaderWriterLock>> _userLocks;
    public UserLockingService()
    {
        _userLocks = new ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, AsyncReaderWriterLock>>();
    }

    public IDisposable WriterLock(ulong guildId, ulong userId)
    {
        var userLock = GetLock(guildId, userId);
        return userLock.WriterLock();
    }

    public IDisposable ReaderLock(ulong guildId, ulong userId)
    {
        var userLock = GetLock(guildId, userId);
        return userLock.ReaderLock();
    }

    public AwaitableDisposable<IDisposable> WriterLockAsync(ulong guildId, ulong userId)
    {
        var userLock = GetLock(guildId, userId);
        return userLock.WriterLockAsync();
    }

    public AwaitableDisposable<IDisposable> ReaderLockAsync(ulong guildId, ulong userId)
    {
        var userLock = GetLock(guildId, userId);
        return userLock.ReaderLockAsync();
    }

    public IDisposable WriteLockAllUsers(ulong guildId)
    {
        IDisposable locks = new GroupedDisposables();
        if (_userLocks.TryGetValue(guildId, out var guildLocks))
        {
            locks = GetLocks(guildLocks.Values, userLock => userLock.WriterLock());
        }

        return locks;
    }

    public IDisposable ReadLockAllUsers(ulong guildId)
    {
        IDisposable locks = new GroupedDisposables();
        if (_userLocks.TryGetValue(guildId, out var guildLocks))
        {
            locks = GetLocks(guildLocks.Values, userLock => userLock.ReaderLock());
        }

        return locks;
    }

    public async Task<IDisposable> WriteLockAllUsersAsync(ulong guildId)
    {
        IDisposable locks = new GroupedDisposables();
        if (_userLocks.TryGetValue(guildId, out var guildLocks))
        {
            locks = await GetLocksAsync(guildLocks.Values, async userLock => await userLock.WriterLockAsync());
        }

        return locks;
    }

    public async Task<IDisposable> ReadLockAllUsersAsync(ulong guildId)
    {
        IDisposable locks = new GroupedDisposables();
        if (_userLocks.TryGetValue(guildId, out var guildLocks))
        {
            locks = await GetLocksAsync(guildLocks.Values, async userLock => await userLock.ReaderLockAsync());
        }

        return locks;
    }

    private AsyncReaderWriterLock GetLock(ulong guildId, ulong userId)
    {
        var guildLocks = _userLocks.GetOrAdd(guildId, new ConcurrentDictionary<ulong, AsyncReaderWriterLock>());
        return guildLocks.GetOrAdd(userId, _ => new AsyncReaderWriterLock());
    }

    private static IDisposable GetLocks(ICollection<AsyncReaderWriterLock> locks, Func<AsyncReaderWriterLock, IDisposable> locker)
    {
        var groupedLocks = new GroupedDisposables();
        foreach (var userLock in locks)
        {
            groupedLocks.Add(locker(userLock));
        }
        return groupedLocks;
    }

    private static async Task<IDisposable> GetLocksAsync(ICollection<AsyncReaderWriterLock> locks, Func<AsyncReaderWriterLock, Task<IDisposable>> locker)
    {
        var groupedLocks = new GroupedDisposables();
        foreach (var userLock in locks)
        {
            groupedLocks.Add(await locker(userLock));
        }
        return groupedLocks;
    }
}

public sealed class GroupedDisposables : IDisposable
{
    private readonly List<IDisposable> _disposables = new List<IDisposable>();

    public void Add(IDisposable disposable) => _disposables.Add(disposable);

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
    }
}