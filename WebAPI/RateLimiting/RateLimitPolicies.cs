namespace WebAPI.RateLimiting;

/// <summary>Names of the rate limiting policies used with <c>[EnableRateLimiting(...)]</c>.</summary>
public static class RateLimitPolicies
{
    /// <summary>Endpoints that change data: every POST and DELETE.</summary>
    public const string Writes = "writes";

    /// <summary>Endpoints that call an external service (one controller per service).</summary>
    public const string ExternalServices = "external-services";
}
