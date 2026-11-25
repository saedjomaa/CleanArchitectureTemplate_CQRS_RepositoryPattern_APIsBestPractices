namespace WebAPI.Idempotency;

internal sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    /// <summary>How long a key and its stored response are kept. After that the key can be used again.</summary>
    public int ExpirationHours { get; set; } = 24;
}
