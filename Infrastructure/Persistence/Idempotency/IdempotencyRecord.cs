namespace Infrastructure.Persistence.Idempotency;

/// <summary>
/// One row per Idempotency-Key. Not a domain entity: it is a persistence record owned by Infrastructure.
/// <see cref="StatusCode"/> is null while the first request with the key is still running.
/// </summary>
internal sealed class IdempotencyRecord
{
    public const int RequestHashLength = 64;   // SHA-256 as hex
    public const int MaximumContentTypeLength = 200;
    public const int MaximumLocationLength = 2048;

    public string Key { get; set; } = null!;
    public string RequestHash { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public int? StatusCode { get; set; }
    public string? ContentType { get; set; }
    public string? Location { get; set; }
    public byte[]? Body { get; set; }
}
