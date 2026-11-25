using Application.Common.Abstractions;
using Application.Common.Responses;
using Application.Common.Results;

namespace Application.JsonPlaceholder.Queries.ListUsers;

internal sealed class ListUsersQueryHandler(IJsonPlaceholderClient jsonPlaceholder)
    : IQueryHandler<ListUsersQuery, Result<PagedResult<JsonPlaceholderUserResponse>>>
{
    public Task<Result<PagedResult<JsonPlaceholderUserResponse>>> HandleAsync(
        ListUsersQuery query, CancellationToken cancellationToken) =>
        jsonPlaceholder.ListUsersAsync(query.Page, query.PageSize, cancellationToken);
}
