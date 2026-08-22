using System;
using ManagedCode.Communication.Results;

namespace ManagedCode.Communication;

public partial struct Result
{
    /// <summary>
    ///     Creates a typed failure with the generic fallback problem.
    /// </summary>
    public static Result<T> Fail<T>() => ResultFactoryBridge<Result<T>>.Fail();

    /// <summary>
    ///     Creates a typed failure with the given title.
    /// </summary>
    public static Result<T> Fail<T>(string message) => ResultFactoryBridge<Result<T>>.Fail(message);

    /// <summary>
    ///     Creates a typed failure carrying the given problem.
    /// </summary>
    public static Result<T> Fail<T>(Problem problem) => Result<T>.CreateFailed(problem);

    /// <summary>
    ///     Creates a typed failure identified by an enum error code.
    /// </summary>
    public static Result<T> Fail<T, TEnum>(TEnum code) where TEnum : Enum => ResultFactoryBridge<Result<T>>.Fail(code);

    /// <summary>
    ///     Creates a typed failure identified by an enum error code, with a detail message.
    /// </summary>
    public static Result<T> Fail<T, TEnum>(TEnum code, string detail) where TEnum : Enum
    {
        return ResultFactoryBridge<Result<T>>.Fail(code, detail);
    }

    /// <summary>
    ///     Creates a typed failure from an exception.
    /// </summary>
    public static Result<T> Fail<T>(Exception exception) => ResultFactoryBridge<Result<T>>.Fail(exception);

    /// <summary>
    ///     Creates a typed validation failure from field/message pairs.
    /// </summary>
    public static Result<T> FailValidation<T>(params (string field, string message)[] errors)
    {
        return ResultFactoryBridge<Result<T>>.FailValidation(errors);
    }

    /// <summary>
    ///     Creates a typed 401 Unauthorized failure.
    /// </summary>
    public static Result<T> FailUnauthorized<T>(string? detail = null)
    {
        return ResultFactoryBridge<Result<T>>.FailUnauthorized(detail);
    }

    /// <summary>
    ///     Creates a typed 403 Forbidden failure.
    /// </summary>
    public static Result<T> FailForbidden<T>(string? detail = null)
    {
        return ResultFactoryBridge<Result<T>>.FailForbidden(detail);
    }

    /// <summary>
    ///     Creates a typed 404 Not Found failure.
    /// </summary>
    public static Result<T> FailNotFound<T>(string? detail = null)
    {
        return ResultFactoryBridge<Result<T>>.FailNotFound(detail);
    }

    /// <summary>
    ///     Creates a typed 400 failure for a required value that was null.
    /// </summary>
    public static Result<T> FailNull<T>(string? detail = null)
    {
        return ResultFactoryBridge<Result<T>>.FailNull(detail);
    }

    /// <summary>
    ///     Creates a typed 400 failure for an invalid argument.
    /// </summary>
    public static Result<T> FailArgument<T>(string? detail = null)
    {
        return ResultFactoryBridge<Result<T>>.FailArgument(detail);
    }

    /// <summary>
    ///     Creates a typed 400 failure for an argument outside its allowed range.
    /// </summary>
    public static Result<T> FailOutOfRange<T>(string? detail = null)
    {
        return ResultFactoryBridge<Result<T>>.FailOutOfRange(detail);
    }

    /// <summary>
    ///     Creates a typed 409 failure for an operation that conflicts with the current state.
    /// </summary>
    public static Result<T> FailInvalidState<T>(string? detail = null)
    {
        return ResultFactoryBridge<Result<T>>.FailInvalidState(detail);
    }
}
