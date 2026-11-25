using Application.Common.Responses;
using Application.Common.Results;

namespace Application.Common.Abstractions;

/// <summary>
/// Client for the external JSONPlaceholder API. Implemented in Infrastructure.
/// Every expected failure (not found, timeout, service unavailable, invalid response) is returned
/// as a failed <see cref="Result"/> with an error from <c>ExternalServiceErrors</c>; nothing is thrown.
/// </summary>
public interface IJsonPlaceholderClient
{
    Task<Result<PagedResult<JsonPlaceholderUserResponse>>> ListUsersAsync(
        int page, int pageSize, CancellationToken cancellationToken);

    Task<Result<JsonPlaceholderUserResponse>> GetUserAsync(int id, CancellationToken cancellationToken);

    Task<Result<JsonPlaceholderPostResponse>> CreatePostAsync(
        int userId, string title, string body, CancellationToken cancellationToken);
}
