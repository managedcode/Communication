using System;
using System.Net;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication;

public partial struct Result
{
    /// <summary>
    ///     Creates a failed result.
    /// </summary>
    public static Result Fail()
    {
        return Create(false);
    }

    /// <summary>
    ///     Creates a failed result with a problem.
    /// </summary>
    public static Result Fail(Problem problem)
    {
        return Create(false, problem);
    }


    /// <summary>
    ///     Creates a failed result with a title.
    /// </summary>
    public static Result Fail(string title)
    {
        var problem = Problem.Create(title, title, HttpStatusCode.InternalServerError);
        return Create(false, problem);
    }

    /// <summary>
    ///     Creates a failed result with a title and detail.
    /// </summary>
    public static Result Fail(string title, string detail)
    {
        var problem = Problem.Create(title, detail, HttpStatusCode.InternalServerError);
        return Create(false, problem);
    }

    /// <summary>
    ///     Creates a failed result with a title, detail and status.
    /// </summary>
    public static Result Fail(string title, string detail, HttpStatusCode status)
    {
        var problem = Problem.Create(title, detail, (int)status);
        return Create(false, problem);
    }

    /// <summary>
    ///     Creates a failed result from an exception.
    /// </summary>
    public static Result Fail(Exception exception)
    {
        return Create(false, Problem.Create(exception, (int)HttpStatusCode.InternalServerError));
    }

    /// <summary>
    ///     Creates a failed result from an exception with specific status.
    /// </summary>
    public static Result Fail(Exception exception, HttpStatusCode status)
    {
        return Create(false, Problem.Create(exception, (int)status));
    }

    /// <summary>
    ///     Creates a failed result with validation errors.
    /// </summary>
    public static Result FailValidation(params (string field, string message)[] errors)
    {
        return new Result(false, Problem.Validation(errors));
    }

    /// <summary>
    ///     Creates a failed result for bad request.
    /// </summary>
    public static Result FailBadRequest()
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.BadRequest,
            ProblemConstants.Messages.BadRequest,
            (int)HttpStatusCode.BadRequest);

        return Create(false, problem);
    }

    /// <summary>
    ///     Creates a failed result for bad request with custom detail.
    /// </summary>
    public static Result FailBadRequest(string detail)
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.BadRequest,
            detail,
            (int)HttpStatusCode.BadRequest);

        return Create(false, problem);
    }

    /// <summary>
    ///     Creates a failed result for unauthorized access.
    /// </summary>
    public static Result FailUnauthorized()
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.Unauthorized,
            ProblemConstants.Messages.UnauthorizedAccess,
            (int)HttpStatusCode.Unauthorized);

        return Create(false, problem);
    }

    /// <summary>
    ///     Creates a failed result for unauthorized access with custom detail.
    /// </summary>
    public static Result FailUnauthorized(string detail)
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.Unauthorized,
            detail,
            (int)HttpStatusCode.Unauthorized);

        return Create(false, problem);
    }

    /// <summary>
    ///     Creates a failed result for forbidden access.
    /// </summary>
    public static Result FailForbidden()
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.Forbidden,
            ProblemConstants.Messages.ForbiddenAccess,
            (int)HttpStatusCode.Forbidden);

        return Create(false, problem);
    }

    /// <summary>
    ///     Creates a failed result for forbidden access with custom detail.
    /// </summary>
    public static Result FailForbidden(string detail)
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.Forbidden,
            detail,
            (int)HttpStatusCode.Forbidden);

        return Create(false, problem);
    }

    /// <summary>
    ///     Creates a failed result for not found.
    /// </summary>
    public static Result FailNotFound()
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.NotFound,
            ProblemConstants.Messages.ResourceNotFound,
            (int)HttpStatusCode.NotFound);

        return Create(false, problem);
    }

    /// <summary>
    ///     Creates a failed result for not found with custom detail.
    /// </summary>
    public static Result FailNotFound(string detail)
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.NotFound,
            detail,
            (int)HttpStatusCode.NotFound);

        return Create(false, problem);
    }

    /// <summary>
    ///     Creates a failed result from a custom error enum.
    /// </summary>
    public static Result Fail<TEnum>(TEnum errorCode) where TEnum : Enum
    {
        return Create(false, Problem.Create(errorCode));
    }

    /// <summary>
    ///     Creates a failed result from a custom error enum with detail.
    /// </summary>
    public static Result Fail<TEnum>(TEnum errorCode, string detail) where TEnum : Enum
    {
        return Create(false, Problem.Create(errorCode, detail));
    }

    /// <summary>
    ///     Creates a failed result from a custom error enum with specific HTTP status.
    /// </summary>
    public static Result Fail<TEnum>(TEnum errorCode, HttpStatusCode status) where TEnum : Enum
    {
        return Create(false, Problem.Create(errorCode, errorCode.ToString(), (int)status));
    }

    /// <summary>
    ///     Creates a failed result from a custom error enum with detail and specific HTTP status.
    /// </summary>і
    public static Result Fail<TEnum>(TEnum errorCode, string detail, HttpStatusCode status) where TEnum : Enum
    {
        return Create(false, Problem.Create(errorCode, detail, (int)status));
    }
}
