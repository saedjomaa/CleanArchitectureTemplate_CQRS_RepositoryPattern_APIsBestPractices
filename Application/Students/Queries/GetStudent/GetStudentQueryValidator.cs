using FluentValidation;

namespace Application.Students.Queries.GetStudent;

internal sealed class GetStudentQueryValidator : AbstractValidator<GetStudentQuery>
{
    public GetStudentQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}
