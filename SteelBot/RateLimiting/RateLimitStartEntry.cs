using System;

namespace SteelBot.RateLimiting;

public record struct RateLimitStartEntry(int Uses, DateTimeOffset FirstUse);