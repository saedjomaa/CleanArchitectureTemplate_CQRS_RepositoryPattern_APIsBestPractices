using Application.Common.Abstractions;
using Application.Common.Responses;
using Application.Common.Results;
using Application.Shared.Errors;

namespace Application.Courses.Queries.GetCourse;

internal sealed class GetCourseQueryHandler(ICourseRepository courses)
    : IQueryHandler<GetCourseQuery, Result<CourseResponse>>
{
    public async Task<Result<CourseResponse>> HandleAsync(GetCourseQuery query, CancellationToken cancellationToken)
    {
        var course = await courses.GetByIdAsync(query.Id, cancellationToken);

        return course is null
            ? Result<CourseResponse>.Failure(CourseErrors.NotFound)
            : Result<CourseResponse>.Success(course);
    }
}
