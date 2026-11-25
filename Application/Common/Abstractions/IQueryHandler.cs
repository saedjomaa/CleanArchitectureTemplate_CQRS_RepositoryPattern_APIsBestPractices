using Application.Common.Results;

namespace Application.Common.Abstractions;

public interface IQueryHandler<TQuery, TResult> where TResult : IResult<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
