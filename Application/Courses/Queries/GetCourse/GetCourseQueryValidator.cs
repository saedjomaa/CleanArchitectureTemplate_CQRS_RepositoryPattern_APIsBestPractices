using FluentValidation;

namespace Application.Courses.Queries.GetCourse;

internal sealed class GetCourseQueryValidator : AbstractValidator<GetCourseQuery>
{
    public GetCourseQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}
