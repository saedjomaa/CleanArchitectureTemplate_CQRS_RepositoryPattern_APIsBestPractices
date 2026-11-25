namespace Application.Common.Results;

public interface IResult
{
    bool IsSuccess { get; }
    Error? Error { get; }
}

public interface IResult<TSelf> : IResult where TSelf : IResult<TSelf>
{
    static abstract TSelf Failure(Error error);
}
