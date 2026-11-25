using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace WebAPI.RateLimiting;

/// <summary>
/// ASP.NET Core built-in rate limiting (no package). Three layers, all partitioned by client IP:
/// 1. Global limiter on every request (fixed window).
/// 2. <see cref="RateLimitPolicies.Writes"/> on POST/DELETE (sliding window), on top of the global one.
/// 3. <see cref="RateLimitPolicies.ExternalServices"/> on external-service controllers (token bucket), on top of the global one.
/// A rejected request gets 429 with a ProblemDetails (code RateLimit.Exceeded) and a Retry-After header,
/// and never reaches MVC: no model binding, no idempotency key, no handler, no database.
/// </summary>
internal static class RateLimitingSetup
{
    public const string ErrorCode = "RateLimit.Exceeded";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(RateLimitingSettings.SectionName).Get<RateLimitingSettings>()
            ?? new RateLimitingSettings();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(ClientKey(httpContext), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.Global.PermitLimit,
                    Window = TimeSpan.FromSeconds(settings.Global.WindowSeconds),
                    QueueLimit = 0
                }));

            options.AddPolicy(RateLimitPolicies.Writes, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(ClientKey(httpContext), _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = settings.Writes.PermitLimit,
                    Window = TimeSpan.FromSeconds(settings.Writes.WindowSeconds),
                    SegmentsPerWindow = settings.Writes.SegmentsPerWindow,
                    QueueLimit = 0
                }));

            options.AddPolicy(RateLimitPolicies.ExternalServices, httpContext =>
                RateLimitPartition.GetTokenBucketLimiter(ClientKey(httpContext), _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = settings.ExternalServices.TokenLimit,
                    TokensPerPeriod = settings.ExternalServices.TokensPerPeriod,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(settings.ExternalServices.ReplenishmentPeriodSeconds),
                    AutoReplenishment = true,
                    QueueLimit = 0
                }));

            options.OnRejected = WriteRejectionAsync;
        });

        return services;
    }

    /// <summary>
    /// The client is identified by its IP address (there is no authentication yet).
    /// Behind a reverse proxy, configure ForwardedHeaders first, or every client shares the proxy's IP.
    /// </summary>
    private static string ClientKey(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        var services = httpContext.RequestServices;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(RateLimitingSetup));

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }

        // ProblemDetailsFactory fills type/title only for the statuses in ClientErrorMapping, and 429 is not one of them,
        // so they are set here to keep 429 the same shape as every other error.
        var problem = services.GetRequiredService<ProblemDetailsFactory>().CreateProblemDetails(
            httpContext,
            StatusCodes.Status429TooManyRequests,
            title: "Too Many Requests",
            type: "https://tools.ietf.org/html/rfc6585#section-4",
            detail: "Too many requests. Wait before sending more (see the Retry-After header).");
        problem.Extensions["code"] = ErrorCode;

        var jsonOptions = services.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>().Value.SerializerOptions;
        logger.LogWarning(
            "Rate limit exceeded for {ClientIp}. Responded {StatusCode} with {ProblemDetails}",
            ClientKey(httpContext),
            StatusCodes.Status429TooManyRequests,
            JsonSerializer.Serialize(problem, problem.GetType(), jsonOptions));

        await httpContext.Response.WriteAsJsonAsync(
            problem, problem.GetType(), jsonOptions, "application/problem+json", cancellationToken);
    }
}
