using Application.Common.Pagination;
using FluentValidation;

namespace Application.Enrollments.Queries.ListStudentCourses;

internal sealed class ListStudentCoursesQueryValidator : PageQueryValidator<ListStudentCoursesQuery>
{
    public ListStudentCoursesQueryValidator() : base(query => query.Page, query => query.PageSize)
    {
        RuleFor(query => query.StudentId).NotEmpty();
    }
}
