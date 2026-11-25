using Application.Common.Abstractions;
using Application.Common.Responses;
using Application.Common.Results;
using Application.Shared.Errors;

namespace Application.JsonPlaceholder.Queries.GetUser;

internal sealed class GetUserQueryHandler(IJsonPlaceholderClient jsonPlaceholder)
    : IQueryHandler<GetUserQuery, Result<JsonPlaceholderUserResponse>>
{
    public async Task<Result<JsonPlaceholderUserResponse>> HandleAsync(
        GetUserQuery query, CancellationToken cancellationToken)
    {
        var result = await jsonPlaceholder.GetUserAsync(query.Id, cancellationToken);

        return result.Error == ExternalServiceErrors.NotFound
            ? Result<JsonPlaceholderUserResponse>.Failure(JsonPlaceholderErrors.UserNotFound)
            : result;
    }
}
