using Application.Common.Results;

namespace Application.Shared.Errors;

internal static class CourseErrors
{
    public static readonly Error NotFound = new(
        "Courses.NotFound", "The course was not found.", ErrorType.NotFound);
}
