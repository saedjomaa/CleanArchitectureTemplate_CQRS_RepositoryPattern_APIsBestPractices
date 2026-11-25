using Application.Common.Responses;
using Application.Common.Results;
using Domain.Entities;

namespace Application.Common.Abstractions;

public interface IEnrollmentRepository
{
    void Add(Enrollment enrollment);
    void Remove(Enrollment enrollment);
    Task<bool> ExistsAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken);
    Task<EnrollmentResponse?> GetAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken);
    Task<EnrollmentForCancellation?> GetForCancellationAsync(
        Guid studentId, Guid courseId, CancellationToken cancellationToken);
    Task<PagedResult<StudentCourseResponse>> ListStudentCoursesAsync(
        Guid studentId, int page, int pageSize, CancellationToken cancellationToken);
}

public sealed record EnrollmentForCancellation(Enrollment Enrollment, Course Course);
