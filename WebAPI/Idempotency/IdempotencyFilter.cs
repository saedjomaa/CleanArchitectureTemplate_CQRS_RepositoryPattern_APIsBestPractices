using System.Security.Cryptography;
using System.Text;
using Application.Common.Abstractions;
using Application.Common.Results;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using WebAPI.Diagnostics;

namespace WebAPI.Idempotency;

/// <summary>
/// Resource filter (runs before model binding and the handler) for actions marked <see cref="IdempotentAttribute"/>.
/// 1. Requires a valid Idempotency-Key header (400 otherwise).
/// 2. Hashes the request (method, path, query, raw body) and claims the key in <see cref="IIdempotencyStore"/>.
/// 3. If the key was already used: replays the stored response (same request), returns 422 (different request)
///    or 409 (first request still running).
/// 4. Otherwise runs the action, captures the response and stores it. 5xx responses and exceptions are not
///    stored: the claim is released so the client can retry with the same key.
/// </summary>
internal sealed class IdempotencyFilter(
    IIdempotencyStore store,
    IOptions<IdempotencyOptions> options,
    TimeProvider clock,
    ILogger<IdempotencyFilter> logger) : IAsyncResourceFilter
{
    public const string HeaderName = "Idempotency-Key";
    public const string ReplayedHeaderName = "Idempotency-Replayed";

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var headerValues = httpContext.Request.Headers[HeaderName];
        if (headerValues.Count == 0 || string.IsNullOrWhiteSpace(headerValues[0]))
        {
            context.Result = Result.Failure(IdempotencyErrors.KeyMissing).ToProblemDetails(httpContext);
            return;
        }

        var key = headerValues[0]!;
        if (headerValues.Count > 1 || key.Length > IIdempotencyStore.MaximumKeyLength || !key.All(IsVisibleAscii))
        {
            context.Result = Result.Failure(IdempotencyErrors.KeyInvalid).ToProblemDetails(httpContext);
            return;
        }

        var requestHash = await HashRequestAsync(httpContext.Request, httpContext.RequestAborted);
        var expiresAtUtc = clock.GetUtcNow().UtcDateTime.AddHours(options.Value.ExpirationHours);
        var start = await store.TryStartAsync(key, requestHash, expiresAtUtc, httpContext.RequestAborted);

        switch (start.Status)
        {
            case IdempotencyStartStatus.Completed:
                logger.LogInformation("Replaying the stored response for {IdempotencyKey}", key);
                context.Result = new IdempotentReplayResult(start.Response!);
                return;
            case IdempotencyStartStatus.InProgress:
                context.Result = Result.Failure(IdempotencyErrors.RequestInProgress).ToProblemDetails(httpContext);
                return;
            case IdempotencyStartStatus.RequestMismatch:
                context.Result = Result.Failure(IdempotencyErrors.KeyReused).ToProblemDetails(httpContext);
                return;
        }

        // Started: run the action and capture what it writes, so it can be stored and also sent to the client.
        var originalBody = httpContext.Response.Body;
        using var captured = new MemoryStream();
        httpContext.Response.Body = captured;

        ResourceExecutedContext executed;
        try
        {
            executed = await next();
        }
        catch
        {
            httpContext.Response.Body = originalBody;
            await store.ReleaseAsync(key, CancellationToken.None);
            throw;
        }

        httpContext.Response.Body = originalBody;

        if (executed.Exception is not null && !executed.ExceptionHandled)
        {
            // The exception handler middleware writes the 500 to the original body.
            await store.ReleaseAsync(key, CancellationToken.None);
            return;
        }

        captured.Position = 0;
        await captured.CopyToAsync(originalBody, httpContext.RequestAborted);

        var response = httpContext.Response;
        if (response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            await store.ReleaseAsync(key, CancellationToken.None);
            return;
        }

        // Stored even if the client disconnected: the work is done, a retry must replay it.
        await store.CompleteAsync(
            key,
            new IdempotentResponse(
                response.StatusCode,
                response.ContentType,
                response.Headers.Location.Count > 0 ? response.Headers.Location.ToString() : null,
                captured.ToArray()),
            CancellationToken.None);
    }

    private static bool IsVisibleAscii(char character) => character is > ' ' and < (char)127;

    /// <summary>SHA-256 of "METHOD path?query" + raw body. The body is buffered so model binding can read it again.</summary>
    private static async Task<string> HashRequestAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        request.EnableBuffering();
        using var body = new MemoryStream();
        await request.Body.CopyToAsync(body, cancellationToken);
        request.Body.Position = 0;

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(Encoding.UTF8.GetBytes($"{request.Method} {request.Path}{request.QueryString}\n"));
        hash.AppendData(body.ToArray());
        return Convert.ToHexString(hash.GetHashAndReset());
    }
}
