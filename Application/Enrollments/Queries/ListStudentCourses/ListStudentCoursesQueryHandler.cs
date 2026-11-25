using Application.Common.Abstractions;
using Application.Common.Responses;
using Application.Common.Results;
using Application.Shared.Errors;

namespace Application.Enrollments.Queries.ListStudentCourses;

internal sealed class ListStudentCoursesQueryHandler(IStudentRepository students, IEnrollmentRepository enrollments)
    : IQueryHandler<ListStudentCoursesQuery, Result<PagedResult<StudentCourseResponse>>>
{
    public async Task<Result<PagedResult<StudentCourseResponse>>> HandleAsync(
        ListStudentCoursesQuery query, CancellationToken cancellationToken)
    {
        if (!await students.ExistsAsync(query.StudentId, cancellationToken))
        {
            return Result<PagedResult<StudentCourseResponse>>.Failure(StudentErrors.NotFound);
        }

        var page = await enrollments.ListStudentCoursesAsync(
            query.StudentId, query.Page, query.PageSize, cancellationToken);
        return Result<PagedResult<StudentCourseResponse>>.Success(page);
    }
}
