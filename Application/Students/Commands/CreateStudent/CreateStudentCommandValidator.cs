using Domain.Entities;
using FluentValidation;

namespace Application.Students.Commands.CreateStudent;

internal sealed class CreateStudentCommandValidator : AbstractValidator<CreateStudentCommand>
{
    public CreateStudentCommandValidator()
    {
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(Student.MaximumFullNameLength);
    }
}
