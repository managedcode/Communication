using System;
using System.Net;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication.Results.Factories;

/// <summary>
///     Internal helper that centralises creation of <see cref="Result"/> and <see cref="Result{T}"/> failures.
/// </summary>
internal static class ResultFactory
{
    public static Result Success()
    {
        return Result.CreateSuccess();
    }

    public static Result<T> Success<T>(T value)
    {
        return Result<T>.CreateSuccess(value);
    }

    public static Result<T> Success<T>(Func<T> valueFactory)
    {
        return Result<T>.CreateSuccess(valueFactory());
    }

    public static Result Failure()
    {
        return Result.CreateFailed(Problem.GenericError());
    }

    public static Result Failure(Problem problem)
    {
        return Result.CreateFailed(problem);
    }

    public static Result Failure(string title)
    {
        return Result.CreateFailed(Problem.Create(title, title, HttpStatusCode.InternalServerError));
    }

    public static Result Failure(string title, string detail)
    {
        return Result.CreateFailed(Problem.Create(title, detail, HttpStatusCode.InternalServerError));
    }

    public static Result Failure(string title, string detail, HttpStatusCode status)
    {
        return Result.CreateFailed(Problem.Create(title, detail, (int)status));
    }

    public static Result Failure(Exception exception)
    {
        return Result.CreateFailed(Problem.Create(exception, (int)HttpStatusCode.InternalServerError));
    }

    public static Result Failure(Exception exception, HttpStatusCode status)
    {
        return Result.CreateFailed(Problem.Create(exception, (int)status));
    }

    public static Result Failure<TEnum>(TEnum code) where TEnum : Enum
    {
        return Result.CreateFailed(Problem.Create(code));
    }

    public static Result Failure<TEnum>(TEnum code, string detail) where TEnum : Enum
    {
        return Result.CreateFailed(Problem.Create(code, detail));
    }

    public static Result Failure<TEnum>(TEnum code, HttpStatusCode status) where TEnum : Enum
    {
        return Result.CreateFailed(Problem.Create(code, code.ToString(), (int)status));
    }

    public static Result Failure<TEnum>(TEnum code, string detail, HttpStatusCode status) where TEnum : Enum
    {
        return Result.CreateFailed(Problem.Create(code, detail, (int)status));
    }

    public static Result FailureBadRequest(string? detail = null)
    {
        return Result.CreateFailed(Problem.Create(
            ProblemConstants.Titles.BadRequest,
            detail ?? ProblemConstants.Messages.BadRequest,
            (int)HttpStatusCode.BadRequest));
    }

    public static Result FailureUnauthorized(string? detail = null)
    {
        return Result.CreateFailed(Problem.Create(
            ProblemConstants.Titles.Unauthorized,
            detail ?? ProblemConstants.Messages.UnauthorizedAccess,
            (int)HttpStatusCode.Unauthorized));
    }

    public static Result FailureForbidden(string? detail = null)
    {
        return Result.CreateFailed(Problem.Create(
            ProblemConstants.Titles.Forbidden,
            detail ?? ProblemConstants.Messages.ForbiddenAccess,
            (int)HttpStatusCode.Forbidden));
    }

    public static Result FailureNotFound(string? detail = null)
    {
        return Result.CreateFailed(Problem.Create(
            ProblemConstants.Titles.NotFound,
            detail ?? ProblemConstants.Messages.ResourceNotFound,
            (int)HttpStatusCode.NotFound));
    }

    public static Result FailureValidation(params (string field, string message)[] errors)
    {
        return Result.CreateFailed(Problem.Validation(errors));
    }

    public static Result<T> Failure<T>()
    {
        return Result<T>.CreateFailed(Problem.GenericError());
    }

    public static Result<T> Failure<T>(Problem problem)
    {
        return Result<T>.CreateFailed(problem);
    }

    public static Result<T> Failure<T>(string title)
    {
        return Result<T>.CreateFailed(Problem.Create(title, title, HttpStatusCode.InternalServerError));
    }

    public static Result<T> Failure<T>(string title, string detail)
    {
        return Result<T>.CreateFailed(Problem.Create(title, detail));
    }

    public static Result<T> Failure<T>(string title, string detail, HttpStatusCode status)
    {
        return Result<T>.CreateFailed(Problem.Create(title, detail, (int)status));
    }

    public static Result<T> Failure<T>(Exception exception)
    {
        return Result<T>.CreateFailed(Problem.Create(exception, (int)HttpStatusCode.InternalServerError));
    }

    public static Result<T> Failure<T>(Exception exception, HttpStatusCode status)
    {
        return Result<T>.CreateFailed(Problem.Create(exception, (int)status));
    }

    public static Result<T> FailureValidation<T>(params (string field, string message)[] errors)
    {
        return Result<T>.CreateFailed(Problem.Validation(errors));
    }

    public static Result<T> FailureBadRequest<T>(string? detail = null)
    {
        return Result<T>.CreateFailed(Problem.Create(
            ProblemConstants.Titles.BadRequest,
            detail ?? ProblemConstants.Messages.BadRequest,
            (int)HttpStatusCode.BadRequest));
    }

    public static Result<T> FailureUnauthorized<T>(string? detail = null)
    {
        return Result<T>.CreateFailed(Problem.Create(
            ProblemConstants.Titles.Unauthorized,
            detail ?? ProblemConstants.Messages.UnauthorizedAccess,
            (int)HttpStatusCode.Unauthorized));
    }

    public static Result<T> FailureForbidden<T>(string? detail = null)
    {
        return Result<T>.CreateFailed(Problem.Create(
            ProblemConstants.Titles.Forbidden,
            detail ?? ProblemConstants.Messages.ForbiddenAccess,
            (int)HttpStatusCode.Forbidden));
    }

    public static Result<T> FailureNotFound<T>(string? detail = null)
    {
        return Result<T>.CreateFailed(Problem.Create(
            ProblemConstants.Titles.NotFound,
            detail ?? ProblemConstants.Messages.ResourceNotFound,
            (int)HttpStatusCode.NotFound));
    }

    public static Result<T> Failure<T, TEnum>(TEnum code) where TEnum : Enum
    {
        return Result<T>.CreateFailed(Problem.Create(code));
    }

    public static Result<T> Failure<T, TEnum>(TEnum code, string detail) where TEnum : Enum
    {
        return Result<T>.CreateFailed(Problem.Create(code, detail));
    }

    public static Result<T> Failure<T, TEnum>(TEnum code, HttpStatusCode status) where TEnum : Enum
    {
        return Result<T>.CreateFailed(Problem.Create(code, code.ToString(), (int)status));
    }

    public static Result<T> Failure<T, TEnum>(TEnum code, string detail, HttpStatusCode status) where TEnum : Enum
    {
        return Result<T>.CreateFailed(Problem.Create(code, detail, (int)status));
    }
}
