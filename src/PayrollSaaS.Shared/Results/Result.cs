namespace PayrollSaaS.Shared.Results;

public enum ErrorKind
{
    None = 0,
    Validation,   // -> 422
    NotFound,     // -> 404
    Conflict,     // -> 409
    Forbidden,    // -> 403
    Unauthorized  // -> 401
}

public sealed record Error(ErrorKind Kind, string Code, string Message, IReadOnlyDictionary<string, string[]>? Fields = null)
{
    public static Error Validation(string message, IReadOnlyDictionary<string, string[]>? fields = null)
        => new(ErrorKind.Validation, "validation_failed", message, fields);

    public static Error NotFound(string what) => new(ErrorKind.NotFound, "not_found", $"{what} was not found.");
    public static Error Conflict(string code, string message) => new(ErrorKind.Conflict, code, message);
    public static Error Forbidden(string message) => new(ErrorKind.Forbidden, "forbidden", message);
    public static Error Unauthorized(string message) => new(ErrorKind.Unauthorized, "unauthorized", message);
}

public readonly record struct Result
{
    public bool IsSuccess { get; }
    public Error? Error { get; }

    private Result(bool ok, Error? error) { IsSuccess = ok; Error = error; }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);
    public static implicit operator Result(Error error) => Failure(error);
}

public readonly record struct Result<T>
{
    private readonly T? _value;

    public bool IsSuccess { get; }
    public Error? Error { get; }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot read Value from a failed Result.");

    private Result(bool ok, T? value, Error? error) { IsSuccess = ok; _value = value; Error = error; }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(Error error) => new(false, default, error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
