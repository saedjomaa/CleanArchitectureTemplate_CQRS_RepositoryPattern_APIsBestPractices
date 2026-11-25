using Application.Common.Pagination;

namespace Application.JsonPlaceholder.Queries.ListUsers;

public sealed record ListUsersQuery(int Page = Pagination.DefaultPage, int PageSize = Pagination.DefaultPageSize);
