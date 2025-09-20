using System;
using System.Net;
using ManagedCode.Communication.Constants;
using ManagedCode.Communication.Results.Factories;

namespace ManagedCode.Communication;

public partial struct Result
{
    /// <summary>
    ///     Creates a failed result.
    /// </summary>
    public static Result Fail()
    {
        return ResultFactory.Failure();
    }

    /// <summary>
    ///     Creates a failed result with a problem.
    /// </summary>
    public static Result Fail(Problem problem)
    {
        return ResultFactory.Failure(problem);
    }


    /// <summary>
    ///     Creates a failed result with a title.
    /// </summary>
    public static Result Fail(string title)
    {
        return ResultFactory.Failure(title);
    }

    /// <summary>
    ///     Creates a failed result with a title and detail.
    /// </summary>
    public static Result Fail(string title, string detail)
    {
        return ResultFactory.Failure(title, detail);
    }

    /// <summary>
    ///     Creates a failed result with a title, detail and status.
    /// </summary>
    public static Result Fail(string title, string detail, HttpStatusCode status)
    {
        return ResultFactory.Failure(title, detail, status);
    }

    /// <summary>
    ///     Creates a failed result from an exception.
    /// </summary>
    public static Result Fail(Exception exception)
    {
        return ResultFactory.Failure(exception);
    }

    /// <summary>
    ///     Creates a failed result from an exception with specific status.
    /// </summary>
    public static Result Fail(Exception exception, HttpStatusCode status)
    {
        return ResultFactory.Failure(exception, status);
    }

    /// <summary>
    ///     Creates a failed result with validation errors.
    /// </summary>
    public static Result FailValidation(params (string field, string message)[] errors)
    {
        return ResultFactory.FailureValidation(errors);
    }

    /// <summary>
    ///     Creates a failed result for bad request.
    /// </summary>
    public static Result FailBadRequest()
    {
        return ResultFactory.FailureBadRequest();
    }

    /// <summary>
    ///     Creates a failed result for bad request with custom detail.
    /// </summary>
    public static Result FailBadRequest(string detail)
    {
        return ResultFactory.FailureBadRequest(detail);
    }

    /// <summary>
    ///     Creates a failed result for unauthorized access.
    /// </summary>
    public static Result FailUnauthorized()
    {
        return ResultFactory.FailureUnauthorized();
    }

    /// <summary>
    ///     Creates a failed result for unauthorized access with custom detail.
    /// </summary>
    public static Result FailUnauthorized(string detail)
    {
        return ResultFactory.FailureUnauthorized(detail);
    }

    /// <summary>
    ///     Creates a failed result for forbidden access.
    /// </summary>
    public static Result FailForbidden()
    {
        return ResultFactory.FailureForbidden();
    }

    /// <summary>
    ///     Creates a failed result for forbidden access with custom detail.
    /// </summary>
    public static Result FailForbidden(string detail)
    {
        return ResultFactory.FailureForbidden(detail);
    }

    /// <summary>
    ///     Creates a failed result for not found.
    /// </summary>
    public static Result FailNotFound()
    {
        return ResultFactory.FailureNotFound();
    }

    /// <summary>
    ///     Creates a failed result for not found with custom detail.
    /// </summary>
    public static Result FailNotFound(string detail)
    {
        return ResultFactory.FailureNotFound(detail);
    }

    /// <summary>
    ///     Creates a failed result from a custom error enum.
    /// </summary>
    public static Result Fail<TEnum>(TEnum errorCode) where TEnum : Enum
    {
        return ResultFactory.Failure(errorCode);
    }

    /// <summary>
    ///     Creates a failed result from a custom error enum with detail.
    /// </summary>
    public static Result Fail<TEnum>(TEnum errorCode, string detail) where TEnum : Enum
    {
        return ResultFactory.Failure(errorCode, detail);
    }

    /// <summary>
    ///     Creates a failed result from a custom error enum with specific HTTP status.
    /// </summary>
    public static Result Fail<TEnum>(TEnum errorCode, HttpStatusCode status) where TEnum : Enum
    {
        return ResultFactory.Failure(errorCode, status);
    }

    /// <summary>
    ///     Creates a failed result from a custom error enum with detail and specific HTTP status.
    /// </summary>
    public static Result Fail<TEnum>(TEnum errorCode, string detail, HttpStatusCode status) where TEnum : Enum
    {
        return ResultFactory.Failure(errorCode, detail, status);
    }
}
