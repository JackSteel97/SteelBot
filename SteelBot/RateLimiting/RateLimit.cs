using Microsoft.Extensions.Caching.Memory;
using SteelBot.Exceptions;
using System;

namespace SteelBot.RateLimiting;

public class RateLimit
{
    private readonly string _forCommand;
    private readonly TimeSpan _cooldown;
    private readonly int _maxUses;
    private readonly IMemoryCache _memoryCache;

    public RateLimit(string forCommand, TimeSpan cooldown, int maxUses, IMemoryCache memoryCache)
    {
        _cooldown = cooldown;
        _maxUses = maxUses;
        _memoryCache = memoryCache;
        _forCommand = forCommand;   
    }

    public void ThrowIfExceeded(ulong userId)
    {
        string key = $"rate-limit:command:{_forCommand}:user:{userId}";
        if (_memoryCache.TryGetValue(key, out RateLimitStartEntry entry))
        {
            ThrowIfExceeded(key, entry);
        }
        else
        {
            CreateNewEntry(key);
        }
    }

    private void ThrowIfExceeded(string key, RateLimitStartEntry entry)
    {
        var timeSinceUsed = DateTimeOffset.UtcNow - entry.FirstUse;
        if (timeSinceUsed < _cooldown && entry.Uses >= _maxUses)
        {
            throw new CommandRateLimitedException(_forCommand, _maxUses, _cooldown, GetRemainingCooldownTime(entry.FirstUse));
        } 
        
        IncrementUses(key, entry);
    }

    private TimeSpan GetRemainingCooldownTime(DateTimeOffset firstUse)
    {
        var expiryTime = GetExpiryTime(firstUse);
        return expiryTime - DateTimeOffset.UtcNow;
    }

    private DateTimeOffset GetExpiryTime(DateTimeOffset firstUse)
    {
        return firstUse.Add(_cooldown);
    }

    private void CreateNewEntry(string key)
    {
        var now = DateTimeOffset.UtcNow;
        _memoryCache.Set(key, new RateLimitStartEntry(1, now) , GetExpiryTime(now));
    }

    private void IncrementUses(string key, RateLimitStartEntry entry)
    {
        entry.Uses++;
        _memoryCache.Set(key, entry, GetExpiryTime(entry.FirstUse));
    }


}