using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;

namespace SteelBot.RateLimiting;

public class RateLimitFactory
{
    private readonly IMemoryCache _memoryCache;
    private readonly Dictionary<string, RateLimit> _limitsByCommand;

    public RateLimitFactory(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
        _limitsByCommand = new Dictionary<string, RateLimit>();
    }

    public RateLimit Get(string commandName, int maxUses, TimeSpan cooldown)
    {
        return !_limitsByCommand.TryGetValue(commandName, out var limit) 
            ? Create(commandName, maxUses, cooldown) 
            : limit;
    }
    
    private RateLimit Create(string commandName, int maxUses, TimeSpan cooldown)
    {
        var limit =  new RateLimit(commandName, cooldown, maxUses, _memoryCache);
        _limitsByCommand.Add(commandName, limit);
        return limit;
    }
}