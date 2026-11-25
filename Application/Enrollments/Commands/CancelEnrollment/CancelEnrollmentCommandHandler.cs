using Application.Common.Abstractions;
using Application.Common.Results;
using Application.Shared.Errors;

namespace Application.Enrollments.Commands.CancelEnrollment;

internal sealed class CancelEnrollmentCommandHandler(IEnrollmentRepository enrollments, IUnitOfWork unitOfWork)
    : ICommandHandler<CancelEnrollmentCommand, Result>
{
    public async Task<Result> HandleAsync(CancelEnrollmentCommand command, CancellationToken cancellationToken)
    {
        var registration = await enrollments.GetForCancellationAsync(
            command.StudentId, command.CourseId, cancellationToken);
        if (registration is null)
        {
            return Result.Failure(EnrollmentErrors.NotFound);
        }

        registration.Course.CancelEnrollment(registration.Enrollment);
        enrollments.Remove(registration.Enrollment);
        var result = await unitOfWork.SaveChangesAsync(cancellationToken);
        return result.Error == PersistenceErrors.ConcurrencyConflict
            ? Result.Failure(EnrollmentErrors.ConcurrencyConflict)
            : result;
    }
}
