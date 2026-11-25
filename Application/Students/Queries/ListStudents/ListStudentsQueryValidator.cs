using Application.Common.Pagination;

namespace Application.Students.Queries.ListStudents;

internal sealed class ListStudentsQueryValidator()
    : PageQueryValidator<ListStudentsQuery>(query => query.Page, query => query.PageSize);
