using Application.Common.Pagination;

namespace Application.Enrollments.Queries.ListStudentCourses;

public sealed record ListStudentCoursesQuery(
    Guid StudentId, int Page = Pagination.DefaultPage, int PageSize = Pagination.DefaultPageSize);
