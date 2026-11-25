using Application.Common.Results;

namespace Application.Shared.Errors;

internal static class JsonPlaceholderErrors
{
    public static readonly Error UserNotFound = new(
        "JsonPlaceholder.UserNotFound", "The JSONPlaceholder user was not found.", ErrorType.NotFound);
}
