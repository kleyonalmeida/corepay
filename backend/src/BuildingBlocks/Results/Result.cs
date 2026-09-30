namespace BuildingBlocks.Results;

public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error is not null)
        {
            throw new ArgumentException("Successful results cannot contain an error.", nameof(error));
        }

        if (!isSuccess && error is null)
        {
            throw new ArgumentException("Failed results must contain an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value)
        : base(true, null)
    {
        _value = value;
    }

    private Result(Error error)
        : base(false, error)
    {
        _value = default;
    }

    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public static Result<T> Success(T value) => new(value);

    public new static Result<T> Failure(Error error) => new(error);
}
