using Application.Common.Results;

namespace Application.Shared.Errors;

public static class PersistenceErrors
{
    public static readonly Error ConcurrencyConflict = new(
        "Persistence.ConcurrencyConflict", "The data changed during the request. Try again.", ErrorType.Conflict);

    public static readonly Error DuplicateKey = new(
        "Persistence.DuplicateKey", "A record with the same key already exists.", ErrorType.Conflict);
}
