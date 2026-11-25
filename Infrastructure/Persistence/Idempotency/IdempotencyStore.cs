using Application.Common.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Idempotency;

internal sealed class IdempotencyStore(SchoolDbContext db, TimeProvider clock) : IIdempotencyStore
{
    private const int SqliteConstraintPrimaryKey = 1555;

    public async Task<IdempotencyStart> TryStartAsync(
        string key, string requestHash, DateTime expiresAtUtc, CancellationToken cancellationToken)
    {
        // Two attempts: the second one runs only after an expired record was removed.
        for (var attempt = 0; attempt < 2; attempt++)
        {
            if (await TryClaimAsync(key, requestHash, expiresAtUtc, cancellationToken))
            {
                return new IdempotencyStart(IdempotencyStartStatus.Started);
            }

            var existing = await db.Set<IdempotencyRecord>().AsNoTracking()
                .SingleOrDefaultAsync(record => record.Key == key, cancellationToken);
            if (existing is null)
            {
                continue;   // removed between the insert and the read: try to claim it again
            }

            var now = clock.GetUtcNow().UtcDateTime;
            if (existing.ExpiresAtUtc <= now)
            {
                await db.Set<IdempotencyRecord>()
                    .Where(record => record.Key == key && record.ExpiresAtUtc <= now)
                    .ExecuteDeleteAsync(cancellationToken);
                continue;
            }

            if (existing.RequestHash != requestHash)
            {
                return new IdempotencyStart(IdempotencyStartStatus.RequestMismatch);
            }

            return existing.StatusCode is { } statusCode
                ? new IdempotencyStart(
                    IdempotencyStartStatus.Completed,
                    new IdempotentResponse(statusCode, existing.ContentType, existing.Location, existing.Body ?? []))
                : new IdempotencyStart(IdempotencyStartStatus.InProgress);
        }

        return new IdempotencyStart(IdempotencyStartStatus.InProgress);
    }

    public Task CompleteAsync(string key, IdempotentResponse response, CancellationToken cancellationToken) =>
        db.Set<IdempotencyRecord>()
            .Where(record => record.Key == key)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(record => record.StatusCode, response.StatusCode)
                .SetProperty(record => record.ContentType, response.ContentType)
                .SetProperty(record => record.Location, response.Location)
                .SetProperty(record => record.Body, response.Body), cancellationToken);

    public Task ReleaseAsync(string key, CancellationToken cancellationToken) =>
        db.Set<IdempotencyRecord>()
            .Where(record => record.Key == key && record.StatusCode == null)
            .ExecuteDeleteAsync(cancellationToken);

    /// <summary>Inserts the claim. Returns false when a record with the key already exists.</summary>
    private async Task<bool> TryClaimAsync(
        string key, string requestHash, DateTime expiresAtUtc, CancellationToken cancellationToken)
    {
        var record = new IdempotencyRecord
        {
            Key = key,
            RequestHash = requestHash,
            CreatedAtUtc = clock.GetUtcNow().UtcDateTime,
            ExpiresAtUtc = expiresAtUtc
        };

        // The filter runs before the handler, so the change tracker holds nothing else at this point.
        db.Add(record);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteConstraintPrimaryKey })
        {
            return false;
        }
        finally
        {
            // Never leave the record tracked: the handler's Unit of Work must not see it.
            db.Entry(record).State = EntityState.Detached;
        }
    }
}
