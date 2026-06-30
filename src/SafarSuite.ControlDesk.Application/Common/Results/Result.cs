namespace SafarSuite.ControlDesk.Application.Common.Results;

public sealed class Result<T>
{
    private readonly T? _value;

    private Result(T? value, bool isSuccess, IReadOnlyCollection<ApplicationError> errors)
    {
        _value = value;
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Failure results do not contain a value.");

    public IReadOnlyCollection<ApplicationError> Errors { get; }

    public static Result<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new Result<T>(value, true, []);
    }

    public static Result<T> Failure(IEnumerable<ApplicationError> errors)
    {
        var errorList = errors.ToArray();

        if (errorList.Length == 0)
        {
            throw new ArgumentException("Failure results require at least one error.", nameof(errors));
        }

        return new Result<T>(default, false, errorList);
    }

    public static Result<T> Failure(params ApplicationError[] errors)
    {
        return Failure((IEnumerable<ApplicationError>)errors);
    }
}
