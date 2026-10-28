namespace GoIPGateway.Application.Common.Models;

public enum ErrorType
{
    Failure = 0,
    Unexpected = 1,
    Validation = 2,
    Conflict = 3,
    NotFound = 4,
    Unauthorized = 5,
    Forbidden = 6
}

public record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
    public static readonly Error NullValue = new("Error.NullValue", "The specified result value is null.", ErrorType.Failure);

    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
    public static Error Unexpected(string code, string message) => new(code, message, ErrorType.Unexpected);
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);
}

public class Result
{
    protected Result(bool isSuccess, Error error, List<Error>? validationErrors = null)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Success result cannot have an error.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Failure result must have an error.");

        IsSuccess = isSuccess;
        Error = error;
        ValidationErrors = validationErrors ?? new List<Error>();
    }

    public bool IsSuccess { get; }
    public bool Succeeded => IsSuccess;
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }
    public List<Error> ValidationErrors { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result Failure(string code, string message, ErrorType type = ErrorType.Failure) =>
        new(false, new Error(code, message, type));
    public static Result ValidationFailure(List<Error> errors) =>
        new(false, Error.Validation("General.Validation", "One or more validation errors occurred."), errors);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
    public static Result<TValue> Failure<TValue>(string code, string message, ErrorType type = ErrorType.Failure) =>
        new(default, false, new Error(code, message, type));
    public static Result<TValue> ValidationFailure<TValue>(List<Error> errors) =>
        new(default, false, Error.Validation("General.Validation", "One or more validation errors occurred."), errors);
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error, List<Error>? validationErrors = null)
        : base(isSuccess, error, validationErrors)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failure result.");

    public TValue? ValueOrDefault => _value;
    public TValue? Data => ValueOrDefault;

    public static implicit operator Result<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure<TValue>(Error.NullValue);
}
