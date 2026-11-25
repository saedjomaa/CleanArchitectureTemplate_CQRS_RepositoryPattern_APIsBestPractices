using System.Linq.Expressions;
using Application.Common.Abstractions;
using Application.Common.Responses;
using Application.Common.Results;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

internal sealed class CourseRepository(SchoolDbContext db) : ICourseRepository
{
    private static readonly Expression<Func<Course, CourseResponse>> ToResponse =
        course => new CourseResponse(course.Id, course.Title, course.Capacity, course.EnrolledCount, course.RowVersion);

    public void Add(Course course) => db.Set<Course>().Add(course);

    public Task<Course?> GetForEnrollmentAsync(Guid id, CancellationToken cancellationToken) =>
        db.Set<Course>().SingleOrDefaultAsync(course => course.Id == id, cancellationToken);

    public Task<CourseResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Set<Course>().AsNoTracking()
            .Where(course => course.Id == id)
            .Select(ToResponse)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<PagedResult<CourseResponse>> ListAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        db.Set<Course>().AsNoTracking()
            .OrderBy(course => course.Title)
            .ThenBy(course => course.Id)
            .Select(ToResponse)
            .ToPageAsync(page, pageSize, cancellationToken);
}
