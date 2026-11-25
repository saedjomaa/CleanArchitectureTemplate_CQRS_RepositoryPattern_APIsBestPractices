using Application.Common.Abstractions;
using Application.Common.Responses;
using Application.Common.Results;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

internal sealed class EnrollmentRepository(SchoolDbContext db) : IEnrollmentRepository
{
    public void Add(Enrollment enrollment) => db.Set<Enrollment>().Add(enrollment);

    public void Remove(Enrollment enrollment) => db.Set<Enrollment>().Remove(enrollment);

    public Task<bool> ExistsAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken) =>
        Find(studentId, courseId).AnyAsync(cancellationToken);

    public Task<EnrollmentResponse?> GetAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken) =>
        Find(studentId, courseId).AsNoTracking()
            .Select(enrollment => new EnrollmentResponse(enrollment.StudentId, enrollment.CourseId, enrollment.EnrolledAt))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<EnrollmentForCancellation?> GetForCancellationAsync(
        Guid studentId, Guid courseId, CancellationToken cancellationToken)
    {
        var registration =
            from enrollment in Find(studentId, courseId)
            join course in db.Set<Course>() on enrollment.CourseId equals course.Id
            select new EnrollmentForCancellation(enrollment, course);

        return registration.SingleOrDefaultAsync(cancellationToken);
    }

    public Task<PagedResult<StudentCourseResponse>> ListStudentCoursesAsync(
        Guid studentId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var courses =
            from enrollment in db.Set<Enrollment>().AsNoTracking()
            join course in db.Set<Course>().AsNoTracking() on enrollment.CourseId equals course.Id
            where enrollment.StudentId == studentId
            orderby course.Title, course.Id
            select new StudentCourseResponse(course.Id, course.Title, enrollment.EnrolledAt);

        return courses.ToPageAsync(page, pageSize, cancellationToken);
    }

    private IQueryable<Enrollment> Find(Guid studentId, Guid courseId) =>
        db.Set<Enrollment>().Where(enrollment => enrollment.StudentId == studentId && enrollment.CourseId == courseId);
}
