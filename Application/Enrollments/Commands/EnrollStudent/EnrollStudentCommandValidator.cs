using FluentValidation;

namespace Application.Enrollments.Commands.EnrollStudent;

internal sealed class EnrollStudentCommandValidator : AbstractValidator<EnrollStudentCommand>
{
    public EnrollStudentCommandValidator()
    {
        RuleFor(command => command.StudentId).NotEmpty();
        RuleFor(command => command.CourseId).NotEmpty();
    }
}
