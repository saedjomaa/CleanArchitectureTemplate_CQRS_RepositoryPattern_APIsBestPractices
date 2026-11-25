using FluentValidation;

namespace Application.Enrollments.Commands.CancelEnrollment;

internal sealed class CancelEnrollmentCommandValidator : AbstractValidator<CancelEnrollmentCommand>
{
    public CancelEnrollmentCommandValidator()
    {
        RuleFor(command => command.StudentId).NotEmpty();
        RuleFor(command => command.CourseId).NotEmpty();
    }
}
