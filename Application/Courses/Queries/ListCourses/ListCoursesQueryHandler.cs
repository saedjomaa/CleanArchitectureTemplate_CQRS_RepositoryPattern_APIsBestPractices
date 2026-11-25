using Application.Common.Abstractions;
using Application.Common.Responses;
using Application.Common.Results;

namespace Application.Courses.Queries.ListCourses;

internal sealed class ListCoursesQueryHandler(ICourseRepository courses)
    : IQueryHandler<ListCoursesQuery, Result<PagedResult<CourseResponse>>>
{
    public async Task<Result<PagedResult<CourseResponse>>> HandleAsync(
        ListCoursesQuery query, CancellationToken cancellationToken)
    {
        var page = await courses.ListAsync(query.Page, query.PageSize, cancellationToken);

        return Result<PagedResult<CourseResponse>>.Success(page);
    }
}
