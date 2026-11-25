using Application.Common.Abstractions;
using Application.Common.Results;
using Application.Shared.Errors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

internal sealed class SchoolUnitOfWork(SchoolDbContext db) : IUnitOfWork
{
    private const int SqliteConstraintPrimaryKey = 1555;
    private const int SqliteConstraintUnique = 2067;

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            return Result.Failure(PersistenceErrors.ConcurrencyConflict);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException
            {
                SqliteExtendedErrorCode: SqliteConstraintPrimaryKey or SqliteConstraintUnique
            })
        {
            db.ChangeTracker.Clear();
            return Result.Failure(PersistenceErrors.DuplicateKey);
        }
    }
}
