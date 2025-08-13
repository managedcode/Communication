using System;
using System.Net;
using ManagedCode.Communication.Constants;

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
        return Create(false, default);
    }

    /// <summary>
    ///     Creates a failed result with a specific value.
    /// </summary>
    public static Result<T> Fail(T value)
    {
        return Create(false, value);
    }

    /// <summary>
    ///     Creates a failed result with a problem.
    /// </summary>
    public static Result<T> Fail(Problem problem)
    {
        return Create(false, default, problem);
    }


    /// <summary>
    ///     Creates a failed result with a title.
    /// </summary>
    public static Result<T> Fail(string title)
    {
        var problem = Problem.Create(title, title, (int)HttpStatusCode.InternalServerError);
        return Create(false, default, problem);
    }

    /// <summary>
    ///     Creates a failed result with a title and detail.
    /// </summary>
    public static Result<T> Fail(string title, string detail)
    {
        var problem = Problem.Create(title, detail);
        return Create(false, default, problem);
    }

    /// <summary>
    ///     Creates a failed result with a title, detail and status.
    /// </summary>
    public static Result<T> Fail(string title, string detail, HttpStatusCode status)
    {
        var problem = Problem.Create(title, detail, (int)status);
        return Create(false, default, problem);
    }

    /// <summary>
    ///     Creates a failed result from an exception.
    /// </summary>
    public static Result<T> Fail(Exception exception)
    {
        return new Result<T>(false, default, Problem.Create(exception, (int)HttpStatusCode.InternalServerError));
    }

    /// <summary>
    ///     Creates a failed result from an exception with status.
    /// </summary>
    public static Result<T> Fail(Exception exception, HttpStatusCode status)
    {
        return new Result<T>(false, default, Problem.Create(exception, (int)status));
    }

    /// <summary>
    ///     Creates a failed result with validation errors.
    /// </summary>
    public static Result<T> FailValidation(params (string field, string message)[] errors)
    {
        return new Result<T>(false, default, Problem.Validation(errors));
    }

    /// <summary>
    ///     Creates a failed result for bad request.
    /// </summary>
    public static Result<T> FailBadRequest()
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.BadRequest,
            ProblemConstants.Messages.BadRequest,
            (int)HttpStatusCode.BadRequest);

        return Create(false, default, problem);
    }

    /// <summary>
    ///     Creates a failed result for bad request with custom detail.
    /// </summary>
    public static Result<T> FailBadRequest(string detail)
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.BadRequest,
            detail,
            (int)HttpStatusCode.BadRequest);

        return Create(false, default, problem);
    }

    /// <summary>
    ///     Creates a failed result for unauthorized access.
    /// </summary>
    public static Result<T> FailUnauthorized()
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.Unauthorized,
            ProblemConstants.Messages.UnauthorizedAccess,
            (int)HttpStatusCode.Unauthorized);

        return Create(false, default, problem);
    }

    /// <summary>
    ///     Creates a failed result for unauthorized access with custom detail.
    /// </summary>
    public static Result<T> FailUnauthorized(string detail)
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.Unauthorized,
            detail,
            (int)HttpStatusCode.Unauthorized);

        return Create(false, default, problem);
    }

    /// <summary>
    ///     Creates a failed result for forbidden access.
    /// </summary>
    public static Result<T> FailForbidden()
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.Forbidden,
            ProblemConstants.Messages.ForbiddenAccess,
            (int)HttpStatusCode.Forbidden);

        return Create(false, default, problem);
    }

    /// <summary>
    ///     Creates a failed result for forbidden access with custom detail.
    /// </summary>
    public static Result<T> FailForbidden(string detail)
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.Forbidden,
            detail,
            (int)HttpStatusCode.Forbidden);

        return Create(false, default, problem);
    }

    /// <summary>
    ///     Creates a failed result for not found.
    /// </summary>
    public static Result<T> FailNotFound()
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.NotFound,
            ProblemConstants.Messages.ResourceNotFound,
            (int)HttpStatusCode.NotFound);

        return Create(false, default, problem);
    }

    /// <summary>
    ///     Creates a failed result for not found with custom detail.
    /// </summary>
    public static Result<T> FailNotFound(string detail)
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.NotFound,
            detail,
            (int)HttpStatusCode.NotFound);

        return Create(false, default, problem);
    }

    /// <summary>
    ///     Creates a failed result from a custom error enum.
    /// </summary>
    public static Result<T> Fail<TEnum>(TEnum errorCode) where TEnum : Enum
    {
        return new Result<T>(false, default, Problem.Create(errorCode));
    }

    /// <summary>
    ///     Creates a failed result from a custom error enum with detail.
    /// </summary>
    public static Result<T> Fail<TEnum>(TEnum errorCode, string detail) where TEnum : Enum
    {
        return new Result<T>(false, default, Problem.Create(errorCode, detail));
    }

    /// <summary>
    ///     Creates a failed result from a custom error enum with specific HTTP status.
    /// </summary>
    public static Result<T> Fail<TEnum>(TEnum errorCode, HttpStatusCode status) where TEnum : Enum
    {
        return new Result<T>(false, default, Problem.Create(errorCode, errorCode.ToString(), (int)status));
    }

    /// <summary>
    ///     Creates a failed result from a custom error enum with detail and specific HTTP status.
    /// </summary>
    public static Result<T> Fail<TEnum>(TEnum errorCode, string detail, HttpStatusCode status) where TEnum : Enum
    {
        return new Result<T>(false, default, Problem.Create(errorCode, detail, (int)status));
    }
}
