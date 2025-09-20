using System;
using System.Net;
using ManagedCode.Communication.Constants;
using ManagedCode.Communication.Results.Factories;

namespace ManagedCode.Communication;

/// <summary>
///     Represents a result of an operation with a specific type.
///     This partial class contains methods for creating failed results.
/// </summary>
public partial struct Result<T>
{
    /// <summary>
    ///     Creates a failed result.
    /// </summary>
    public static Result<T> Fail()
    {
        return ResultFactory.Failure<T>();
    }

    /// <summary>
    ///     Creates a failed result with a specific value.
    /// </summary>
    public static Result<T> Fail(T value)
    {
        return Result<T>.CreateFailed(Problem.GenericError(), value);
    }

    /// <summary>
    ///     Creates a failed result with a problem.
    /// </summary>
    public static Result<T> Fail(Problem problem)
    {
        return ResultFactory.Failure<T>(problem);
    }


    /// <summary>
    ///     Creates a failed result with a title.
    /// </summary>
    public static Result<T> Fail(string title)
    {
        return ResultFactory.Failure<T>(title);
    }

    /// <summary>
    ///     Creates a failed result with a title and detail.
    /// </summary>
    public static Result<T> Fail(string title, string detail)
    {
        return ResultFactory.Failure<T>(title, detail);
    }

    /// <summary>
    ///     Creates a failed result with a title, detail and status.
    /// </summary>
    public static Result<T> Fail(string title, string detail, HttpStatusCode status)
    {
        return ResultFactory.Failure<T>(title, detail, status);
    }

    /// <summary>
    ///     Creates a failed result from an exception.
    /// </summary>
    public static Result<T> Fail(Exception exception)
    {
        return ResultFactory.Failure<T>(exception);
    }

    /// <summary>
    ///     Creates a failed result from an exception with status.
    /// </summary>
    public static Result<T> Fail(Exception exception, HttpStatusCode status)
    {
        return ResultFactory.Failure<T>(exception, status);
    }

    /// <summary>
    ///     Creates a failed result with validation errors.
    /// </summary>
    public static Result<T> FailValidation(params (string field, string message)[] errors)
    {
        return ResultFactory.FailureValidation<T>(errors);
    }

    /// <summary>
    ///     Creates a failed result for bad request.
    /// </summary>
    public static Result<T> FailBadRequest()
    {
        return ResultFactory.FailureBadRequest<T>();
    }

    /// <summary>
    ///     Creates a failed result for bad request with custom detail.
    /// </summary>
    public static Result<T> FailBadRequest(string detail)
    {
        return ResultFactory.FailureBadRequest<T>(detail);
    }

    /// <summary>
    ///     Creates a failed result for unauthorized access.
    /// </summary>
    public static Result<T> FailUnauthorized()
    {
        return ResultFactory.FailureUnauthorized<T>();
    }

    /// <summary>
    ///     Creates a failed result for unauthorized access with custom detail.
    /// </summary>
    public static Result<T> FailUnauthorized(string detail)
    {
        return ResultFactory.FailureUnauthorized<T>(detail);
    }

    /// <summary>
    ///     Creates a failed result for forbidden access.
    /// </summary>
    public static Result<T> FailForbidden()
    {
        return ResultFactory.FailureForbidden<T>();
    }

    /// <summary>
    ///     Creates a failed result for forbidden access with custom detail.
    /// </summary>
    public static Result<T> FailForbidden(string detail)
    {
        return ResultFactory.FailureForbidden<T>(detail);
    }

    /// <summary>
    ///     Creates a failed result for not found.
    /// </summary>
    public static Result<T> FailNotFound()
    {
        return ResultFactory.FailureNotFound<T>();
    }

    /// <summary>
    ///     Creates a failed result for not found with custom detail.
    /// </summary>
    public static Result<T> FailNotFound(string detail)
    {
        return ResultFactory.FailureNotFound<T>(detail);
    }

    /// <summary>
    ///     Creates a failed result from a custom error enum.
    /// </summary>
    public static Result<T> Fail<TEnum>(TEnum errorCode) where TEnum : Enum
    {
        return ResultFactory.Failure<T, TEnum>(errorCode);
    }

    /// <summary>
    ///     Creates a failed result from a custom error enum with detail.
    /// </summary>
    public static Result<T> Fail<TEnum>(TEnum errorCode, string detail) where TEnum : Enum
    {
        return ResultFactory.Failure<T, TEnum>(errorCode, detail);
    }

    /// <summary>
    ///     Creates a failed result from a custom error enum with specific HTTP status.
    /// </summary>
    public static Result<T> Fail<TEnum>(TEnum errorCode, HttpStatusCode status) where TEnum : Enum
    {
        return ResultFactory.Failure<T, TEnum>(errorCode, status);
    }

    /// <summary>
    ///     Creates a failed result from a custom error enum with detail and specific HTTP status.
    /// </summary>
    public static Result<T> Fail<TEnum>(TEnum errorCode, string detail, HttpStatusCode status) where TEnum : Enum
    {
        return ResultFactory.Failure<T, TEnum>(errorCode, detail, status);
    }
}
