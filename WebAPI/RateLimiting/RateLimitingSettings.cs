namespace WebAPI.RateLimiting;

/// <summary>Rate limits from the <c>RateLimiting</c> section of appsettings. Every limit is per client IP.</summary>
internal sealed class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    /// <summary>Global limit on every request (fixed window).</summary>
    public FixedWindowSettings Global { get; set; } = new();

    /// <summary>Stricter limit on endpoints that change data (POST, DELETE) (sliding window).</summary>
    public SlidingWindowSettings Writes { get; set; } = new();

    /// <summary>Limit on endpoints that call an external service (token bucket: allows a short burst, then a steady rate).</summary>
    public TokenBucketSettings ExternalServices { get; set; } = new();
}

internal sealed class FixedWindowSettings
{
    public int PermitLimit { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60;
}

internal sealed class SlidingWindowSettings
{
    public int PermitLimit { get; set; } = 10;
    public int WindowSeconds { get; set; } = 60;
    public int SegmentsPerWindow { get; set; } = 6;
}

internal sealed class TokenBucketSettings
{
    public int TokenLimit { get; set; } = 10;
    public int TokensPerPeriod { get; set; } = 1;
    public int ReplenishmentPeriodSeconds { get; set; } = 6;
}
