using Application.Common.Responses;
using Application.Common.Results;
using Domain.Entities;

namespace Application.Common.Abstractions;

public interface ICourseRepository
{
    void Add(Course course);
    Task<Course?> GetForEnrollmentAsync(Guid id, CancellationToken cancellationToken);
    Task<CourseResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<CourseResponse>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
}
