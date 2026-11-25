namespace Application.Common.Results;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    UnprocessableEntity,
    BadGateway,
    GatewayTimeout
}

public sealed record Error(
    string Code,
    string Description,
    ErrorType Type,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);
