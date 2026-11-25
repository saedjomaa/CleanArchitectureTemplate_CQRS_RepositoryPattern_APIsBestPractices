namespace Application.Common.Abstractions;

/// <summary>
/// Persists the outcome of requests sent with an <c>Idempotency-Key</c>, so a retried request
/// returns the first response instead of running again. Implemented in Infrastructure; used by the
/// WebAPI idempotency filter. Handlers never use it.
/// </summary>
public interface IIdempotencyStore
{
    const int MaximumKeyLength = 100;

    /// <summary>
    /// Atomically claims <paramref name="key"/> for this request. Only one request can claim a key;
    /// an expired claim is removed and the key can be claimed again.
    /// </summary>
    Task<IdempotencyStart> TryStartAsync(
        string key, string requestHash, DateTime expiresAtUtc, CancellationToken cancellationToken);

    /// <summary>Stores the response of a claimed key, so later requests with the same key replay it.</summary>
    Task CompleteAsync(string key, IdempotentResponse response, CancellationToken cancellationToken);

    /// <summary>Removes an unfinished claim (the request failed unexpectedly), so the client can retry.</summary>
    Task ReleaseAsync(string key, CancellationToken cancellationToken);
}

public enum IdempotencyStartStatus
{
    /// <summary>The key was free and is now claimed by this request: run it.</summary>
    Started,

    /// <summary>A request with this key already finished: replay <see cref="IdempotencyStart.Response"/>.</summary>
    Completed,

    /// <summary>A request with this key is still running.</summary>
    InProgress,

    /// <summary>The key was already used for a different request (other method, path or body).</summary>
    RequestMismatch
}

public sealed record IdempotencyStart(IdempotencyStartStatus Status, IdempotentResponse? Response = null);

/// <summary>A stored response, replayed byte for byte.</summary>
public sealed record IdempotentResponse(int StatusCode, string? ContentType, string? Location, byte[] Body);
