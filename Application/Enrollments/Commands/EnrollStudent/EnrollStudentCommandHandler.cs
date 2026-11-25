using Application.Common.Abstractions;
using Application.Common.Results;
using Application.Shared.Errors;

namespace Application.Enrollments.Commands.EnrollStudent;

internal sealed class EnrollStudentCommandHandler(
    IStudentRepository students,
    ICourseRepository courses,
    IEnrollmentRepository enrollments,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
    : ICommandHandler<EnrollStudentCommand, Result>
{
    public async Task<Result> HandleAsync(EnrollStudentCommand command, CancellationToken cancellationToken)
    {
        if (!await students.ExistsAsync(command.StudentId, cancellationToken))
        {
            return Result.Failure(StudentErrors.NotFound);
        }

        var course = await courses.GetForEnrollmentAsync(command.CourseId, cancellationToken);
        if (course is null)
        {
            return Result.Failure(CourseErrors.NotFound);
        }

        if (await enrollments.ExistsAsync(command.StudentId, command.CourseId, cancellationToken))
        {
            return Result.Failure(EnrollmentErrors.AlreadyEnrolled);
        }

        var enrollment = course.TryEnroll(command.StudentId, clock.GetUtcNow());
        if (enrollment is null)
        {
            return Result.Failure(EnrollmentErrors.CourseFull);
        }

        enrollments.Add(enrollment);
        var result = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (result.Error == PersistenceErrors.DuplicateKey)
        {
            return Result.Failure(EnrollmentErrors.AlreadyEnrolled);
        }

        return result.Error == PersistenceErrors.ConcurrencyConflict
            ? Result.Failure(EnrollmentErrors.ConcurrencyConflict)
            : result;
    }
}
