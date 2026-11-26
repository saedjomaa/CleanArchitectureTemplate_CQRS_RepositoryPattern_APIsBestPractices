namespace WebAPI.Caching;

/// <summary>Cache durations from the <c>Caching</c> section of appsettings.</summary>
internal sealed class CachingSettings
{
    public const string SectionName = "Caching";

    /// <summary>How long a GET response over the application's own data stays cached (invalidated earlier by writes).</summary>
    public int LocalDataSeconds { get; set; } = 60;

    /// <summary>How long a GET response from an external service stays cached (expiration only).</summary>
    public int ExternalServicesSeconds { get; set; } = 300;
}
