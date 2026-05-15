namespace Rbac.Shared;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string[] Errors { get; }

    protected Result(bool isSuccess, params string[] errors)
    {
        IsSuccess = isSuccess;
        Errors = errors ?? Array.Empty<string>();
    }

    public static Result Success() => new(true);
    public static Result Failure(params string[] errors) => new(false, errors);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(true)
    {
        Value = value;
    }

    private Result(params string[] errors) : base(false, errors) { }

    public static Result<T> Success(T value) => new(value);
    public static new Result<T> Failure(params string[] errors) => new(errors);
}
