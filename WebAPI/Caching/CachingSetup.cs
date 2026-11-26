namespace WebAPI.Caching;

/// <summary>
/// ASP.NET Core built-in Output Caching (no package): whole GET responses are stored per URL (path with version
/// and query string) and served again before MVC runs. Only endpoints marked [OutputCache] are cached, and only
/// 200 responses (errors are never cached). The store is in memory; it can be moved to Redis without changing
/// endpoints (Microsoft.AspNetCore.OutputCaching.StackExchangeRedis).
/// </summary>
internal static class CachingSetup
{
    public static IServiceCollection AddApiCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(CachingSettings.SectionName).Get<CachingSettings>() ?? new CachingSettings();

        services.AddOutputCache(options =>
        {
            options.AddPolicy(CachePolicies.LocalData, policy => policy
                .Expire(TimeSpan.FromSeconds(settings.LocalDataSeconds)));

            options.AddPolicy(CachePolicies.ExternalServices, policy => policy
                .Expire(TimeSpan.FromSeconds(settings.ExternalServicesSeconds)));
        });

        return services;
    }
}
