using Application.Common.Pagination;

namespace Application.Courses.Queries.ListCourses;

internal sealed class ListCoursesQueryValidator()
    : PageQueryValidator<ListCoursesQuery>(query => query.Page, query => query.PageSize);
