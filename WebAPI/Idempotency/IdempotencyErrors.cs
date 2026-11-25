using Application.Common.Abstractions;
using Application.Common.Results;

namespace WebAPI.Idempotency;

internal static class IdempotencyErrors
{
    public static readonly Error KeyMissing = new(
        "Idempotency.KeyMissing",
        $"The {IdempotencyFilter.HeaderName} header is required for this request.",
        ErrorType.Validation);

    public static readonly Error KeyInvalid = new(
        "Idempotency.KeyInvalid",
        $"The {IdempotencyFilter.HeaderName} header must be a single value of 1 to {IIdempotencyStore.MaximumKeyLength} visible ASCII characters (a UUID is recommended).",
        ErrorType.Validation);

    public static readonly Error KeyReused = new(
        "Idempotency.KeyReused",
        $"This {IdempotencyFilter.HeaderName} was already used for a different request. Use a new key for a new request.",
        ErrorType.UnprocessableEntity);

    public static readonly Error RequestInProgress = new(
        "Idempotency.RequestInProgress",
        $"A request with this {IdempotencyFilter.HeaderName} is still being processed. Retry later.",
        ErrorType.Conflict);
}
