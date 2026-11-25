using Application.Common.Pagination;

namespace Application.JsonPlaceholder.Queries.ListUsers;

internal sealed class ListUsersQueryValidator()
    : PageQueryValidator<ListUsersQuery>(query => query.Page, query => query.PageSize);
