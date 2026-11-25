using Application.Common.Results;

namespace Application.Shared.Errors;

internal static class StudentErrors
{
    public static readonly Error NotFound = new(
        "Students.NotFound", "The student was not found.", ErrorType.NotFound);
}
