using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Idempotency;

/// <summary>
/// Marks a controller action as idempotent: the request must carry an <c>Idempotency-Key</c> header,
/// and a retried request with the same key returns the first response instead of running again.
/// Put it on every action that creates something (POST).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotentAttribute() : TypeFilterAttribute(typeof(IdempotencyFilter));
