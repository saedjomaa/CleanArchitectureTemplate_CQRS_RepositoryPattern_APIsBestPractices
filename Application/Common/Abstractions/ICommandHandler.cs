using Application.Common.Results;

namespace Application.Common.Abstractions;

public interface ICommandHandler<TCommand, TResult> where TResult : IResult<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
