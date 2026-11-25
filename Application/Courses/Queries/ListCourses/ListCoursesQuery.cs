using Application.Common.Pagination;

namespace Application.Courses.Queries.ListCourses;

public sealed record ListCoursesQuery(int Page = Pagination.DefaultPage, int PageSize = Pagination.DefaultPageSize);
