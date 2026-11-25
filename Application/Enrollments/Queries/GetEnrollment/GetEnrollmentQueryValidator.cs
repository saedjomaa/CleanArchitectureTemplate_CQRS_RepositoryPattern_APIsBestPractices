using FluentValidation;

namespace Application.Enrollments.Queries.GetEnrollment;

internal sealed class GetEnrollmentQueryValidator : AbstractValidator<GetEnrollmentQuery>
{
    public GetEnrollmentQueryValidator()
    {
        RuleFor(query => query.StudentId).NotEmpty();
        RuleFor(query => query.CourseId).NotEmpty();
    }
}
