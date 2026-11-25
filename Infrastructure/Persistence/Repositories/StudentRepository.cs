using System.Linq.Expressions;
using Application.Common.Abstractions;
using Application.Common.Responses;
using Application.Common.Results;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

internal sealed class StudentRepository(SchoolDbContext db) : IStudentRepository
{
    private static readonly Expression<Func<Student, StudentResponse>> ToResponse =
        student => new StudentResponse(student.Id, student.FullName);

    public void Add(Student student) => db.Set<Student>().Add(student);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        db.Set<Student>().AnyAsync(student => student.Id == id, cancellationToken);

    public Task<StudentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Set<Student>().AsNoTracking()
            .Where(student => student.Id == id)
            .Select(ToResponse)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<PagedResult<StudentResponse>> ListAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        db.Set<Student>().AsNoTracking()
            .OrderBy(student => student.FullName)
            .ThenBy(student => student.Id)
            .Select(ToResponse)
            .ToPageAsync(page, pageSize, cancellationToken);
}
