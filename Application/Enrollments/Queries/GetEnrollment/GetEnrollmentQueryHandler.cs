using Application.Common.Abstractions;
using Application.Common.Responses;
using Application.Common.Results;
using Application.Shared.Errors;

namespace Application.Enrollments.Queries.GetEnrollment;

internal sealed class GetEnrollmentQueryHandler(IEnrollmentRepository enrollments)
    : IQueryHandler<GetEnrollmentQuery, Result<EnrollmentResponse>>
{
    public async Task<Result<EnrollmentResponse>> HandleAsync(
        GetEnrollmentQuery query, CancellationToken cancellationToken)
    {
        var enrollment = await enrollments.GetAsync(query.StudentId, query.CourseId, cancellationToken);

        return enrollment is null
            ? Result<EnrollmentResponse>.Failure(EnrollmentErrors.NotFound)
            : Result<EnrollmentResponse>.Success(enrollment);
    }
}
