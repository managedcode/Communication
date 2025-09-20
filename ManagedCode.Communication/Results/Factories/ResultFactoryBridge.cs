using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ManagedCode.Communication;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication.Results;

/// <summary>
/// Shared helper that centralises creation of failure <see cref="Problem"/> instances for result factories.
/// </summary>
internal static class ResultFactoryBridge
{
    public static TSelf Succeed<TSelf>()
        where TSelf : struct, IResultFactory<TSelf>
    {
        return TSelf.Succeed();
    }

    public static TSelf Fail<TSelf>()
        where TSelf : struct, IResultFactory<TSelf>
    {
        return TSelf.Fail(Problem.GenericError());
    }

    public static TSelf Fail<TSelf>(Problem problem)
        where TSelf : struct, IResultFactory<TSelf>
    {
        return TSelf.Fail(problem);
    }

    public static TSelf Fail<TSelf>(string title)
        where TSelf : struct, IResultFactory<TSelf>
    {
        return TSelf.Fail(Problem.Create(title, title, (int)HttpStatusCode.InternalServerError));
    }

    public static TSelf Fail<TSelf>(string title, string detail)
        where TSelf : struct, IResultFactory<TSelf>
    {
        return TSelf.Fail(Problem.Create(title, detail));
    }

    public static TSelf Fail<TSelf>(string title, string detail, HttpStatusCode status)
        where TSelf : struct, IResultFactory<TSelf>
    {
        return TSelf.Fail(Problem.Create(title, detail, (int)status));
    }

    public static TSelf Fail<TSelf>(Exception exception)
        where TSelf : struct, IResultFactory<TSelf>
    {
        return TSelf.Fail(Problem.Create(exception, (int)HttpStatusCode.InternalServerError));
    }

    public static TSelf Fail<TSelf>(Exception exception, HttpStatusCode status)
        where TSelf : struct, IResultFactory<TSelf>
    {
        return TSelf.Fail(Problem.Create(exception, (int)status));
    }

    public static TSelf Fail<TSelf, TEnum>(TEnum errorCode)
        where TSelf : struct, IResultFactory<TSelf>
        where TEnum : Enum
    {
        return TSelf.Fail(Problem.Create(errorCode));
    }

    public static TSelf Fail<TSelf, TEnum>(TEnum errorCode, string detail)
        where TSelf : struct, IResultFactory<TSelf>
        where TEnum : Enum
    {
        return TSelf.Fail(Problem.Create(errorCode, detail));
    }

    public static TSelf Fail<TSelf, TEnum>(TEnum errorCode, HttpStatusCode status)
        where TSelf : struct, IResultFactory<TSelf>
        where TEnum : Enum
    {
        return TSelf.Fail(Problem.Create(errorCode, errorCode.ToString(), (int)status));
    }

    public static TSelf Fail<TSelf, TEnum>(TEnum errorCode, string detail, HttpStatusCode status)
        where TSelf : struct, IResultFactory<TSelf>
        where TEnum : Enum
    {
        return TSelf.Fail(Problem.Create(errorCode, detail, (int)status));
    }

    public static TSelf FailBadRequest<TSelf>(string? detail = null)
        where TSelf : struct, IResultFactory<TSelf>
    {
        return TSelf.Fail(Problem.Create(
            ProblemConstants.Titles.BadRequest,
            detail ?? ProblemConstants.Messages.BadRequest,
            (int)HttpStatusCode.BadRequest));
    }

    public static TSelf FailUnauthorized<TSelf>(string? detail = null)
        where TSelf : struct, IResultFactory<TSelf>
    {
        return TSelf.Fail(Problem.Create(
            ProblemConstants.Titles.Unauthorized,
            detail ?? ProblemConstants.Messages.UnauthorizedAccess,
            (int)HttpStatusCode.Unauthorized));
    }

    public static TSelf FailForbidden<TSelf>(string? detail = null)
        where TSelf : struct, IResultFactory<TSelf>
    {
        return TSelf.Fail(Problem.Create(
            ProblemConstants.Titles.Forbidden,
            detail ?? ProblemConstants.Messages.ForbiddenAccess,
            (int)HttpStatusCode.Forbidden));
    }

    public static TSelf FailNotFound<TSelf>(string? detail = null)
        where TSelf : struct, IResultFactory<TSelf>
    {
        return TSelf.Fail(Problem.Create(
            ProblemConstants.Titles.NotFound,
            detail ?? ProblemConstants.Messages.ResourceNotFound,
            (int)HttpStatusCode.NotFound));
    }

    public static TSelf FailValidation<TSelf>(params (string field, string message)[] errors)
        where TSelf : struct, IResultFactory<TSelf>
    {
        return TSelf.Fail(Problem.Validation(errors));
    }

    public static TSelf Invalid<TSelf>()
        where TSelf : struct, IResultFactory<TSelf>
    {
        return FailValidation<TSelf>(("message", nameof(Invalid)));
    }

    public static TSelf Invalid<TSelf, TEnum>(TEnum code)
        where TSelf : struct, IResultFactory<TSelf>
        where TEnum : Enum
    {
        return Invalid<TSelf, TEnum>(code, ("message", nameof(Invalid)));
    }

    public static TSelf Invalid<TSelf>(string message)
        where TSelf : struct, IResultFactory<TSelf>
    {
        return FailValidation<TSelf>((nameof(message), message));
    }

    public static TSelf Invalid<TSelf, TEnum>(TEnum code, string message)
        where TSelf : struct, IResultFactory<TSelf>
        where TEnum : Enum
    {
        return Invalid<TSelf, TEnum>(code, (nameof(message), message));
    }

    public static TSelf Invalid<TSelf>(string key, string value)
        where TSelf : struct, IResultFactory<TSelf>
    {
        return FailValidation<TSelf>((key, value));
    }

    public static TSelf Invalid<TSelf, TEnum>(TEnum code, string key, string value)
        where TSelf : struct, IResultFactory<TSelf>
        where TEnum : Enum
    {
        return Invalid<TSelf, TEnum>(code, (key, value));
    }

    public static TSelf Invalid<TSelf>(IEnumerable<KeyValuePair<string, string>> values)
        where TSelf : struct, IResultFactory<TSelf>
    {
        var entries = values?.Select(pair => (pair.Key, pair.Value)).ToArray()
                      ?? Array.Empty<(string field, string message)>();
        return FailValidation<TSelf>(entries);
    }

    public static TSelf Invalid<TSelf, TEnum>(TEnum code, IEnumerable<KeyValuePair<string, string>> values)
        where TSelf : struct, IResultFactory<TSelf>
        where TEnum : Enum
    {
        var entries = values?.Select(pair => (pair.Key, pair.Value)).ToArray()
                      ?? Array.Empty<(string field, string message)>();
        var problem = Problem.Validation(entries);
        problem.ErrorCode = code.ToString();
        return TSelf.Fail(problem);
    }

    private static TSelf Invalid<TSelf, TEnum>(TEnum code, (string field, string message) entry)
        where TSelf : struct, IResultFactory<TSelf>
        where TEnum : Enum
    {
        var problem = Problem.Validation(new[] { entry });
        problem.ErrorCode = code.ToString();
        return TSelf.Fail(problem);
    }
}

/// <summary>
/// Simplified facade that exposes the shared functionality purely through the target result type.
/// </summary>
internal static class ResultFactoryBridge<TSelf>
    where TSelf : struct, IResultFactory<TSelf>
{
    public static TSelf Succeed() => ResultFactoryBridge.Succeed<TSelf>();

    public static TSelf Fail() => ResultFactoryBridge.Fail<TSelf>();

    public static TSelf Fail(Problem problem) => ResultFactoryBridge.Fail<TSelf>(problem);

    public static TSelf Fail(string title) => ResultFactoryBridge.Fail<TSelf>(title);

    public static TSelf Fail(string title, string detail) => ResultFactoryBridge.Fail<TSelf>(title, detail);

    public static TSelf Fail(string title, string detail, HttpStatusCode status)
    {
        return ResultFactoryBridge.Fail<TSelf>(title, detail, status);
    }

    public static TSelf Fail(Exception exception) => ResultFactoryBridge.Fail<TSelf>(exception);

    public static TSelf Fail(Exception exception, HttpStatusCode status)
    {
        return ResultFactoryBridge.Fail<TSelf>(exception, status);
    }

    public static TSelf Fail<TEnum>(TEnum errorCode) where TEnum : Enum
    {
        return ResultFactoryBridge.Fail<TSelf, TEnum>(errorCode);
    }

    public static TSelf Fail<TEnum>(TEnum errorCode, string detail) where TEnum : Enum
    {
        return ResultFactoryBridge.Fail<TSelf, TEnum>(errorCode, detail);
    }

    public static TSelf Fail<TEnum>(TEnum errorCode, HttpStatusCode status) where TEnum : Enum
    {
        return ResultFactoryBridge.Fail<TSelf, TEnum>(errorCode, status);
    }

    public static TSelf Fail<TEnum>(TEnum errorCode, string detail, HttpStatusCode status) where TEnum : Enum
    {
        return ResultFactoryBridge.Fail<TSelf, TEnum>(errorCode, detail, status);
    }

    public static TSelf FailBadRequest(string? detail = null)
    {
        return ResultFactoryBridge.FailBadRequest<TSelf>(detail);
    }

    public static TSelf FailUnauthorized(string? detail = null)
    {
        return ResultFactoryBridge.FailUnauthorized<TSelf>(detail);
    }

    public static TSelf FailForbidden(string? detail = null)
    {
        return ResultFactoryBridge.FailForbidden<TSelf>(detail);
    }

    public static TSelf FailNotFound(string? detail = null)
    {
        return ResultFactoryBridge.FailNotFound<TSelf>(detail);
    }

    public static TSelf FailValidation(params (string field, string message)[] errors)
    {
        return ResultFactoryBridge.FailValidation<TSelf>(errors);
    }

    public static TSelf Invalid() => ResultFactoryBridge.Invalid<TSelf>();

    public static TSelf Invalid<TEnum>(TEnum code) where TEnum : Enum
    {
        return ResultFactoryBridge.Invalid<TSelf, TEnum>(code);
    }

    public static TSelf Invalid(string message)
    {
        return ResultFactoryBridge.Invalid<TSelf>(message);
    }

    public static TSelf Invalid<TEnum>(TEnum code, string message) where TEnum : Enum
    {
        return ResultFactoryBridge.Invalid<TSelf, TEnum>(code, message);
    }

    public static TSelf Invalid(string key, string value)
    {
        return ResultFactoryBridge.Invalid<TSelf>(key, value);
    }

    public static TSelf Invalid<TEnum>(TEnum code, string key, string value) where TEnum : Enum
    {
        return ResultFactoryBridge.Invalid<TSelf, TEnum>(code, key, value);
    }

    public static TSelf Invalid(IEnumerable<KeyValuePair<string, string>> values)
    {
        return ResultFactoryBridge.Invalid<TSelf>(values);
    }

    public static TSelf Invalid<TEnum>(TEnum code, IEnumerable<KeyValuePair<string, string>> values) where TEnum : Enum
    {
        return ResultFactoryBridge.Invalid<TSelf, TEnum>(code, values);
    }
}

/// <summary>
/// Helper that forwards value factory calls to <see cref="IResultValueFactory{TSelf, TValue}"/> members.
/// </summary>
internal static class ResultValueFactoryBridge<TSelf, TValue>
    where TSelf : struct, IResultValueFactory<TSelf, TValue>
{
    public static TSelf Succeed(TValue value) => TSelf.Succeed(value);

    public static TSelf Succeed(Func<TValue> valueFactory)
    {
        if (valueFactory is null)
        {
            throw new ArgumentNullException(nameof(valueFactory));
        }

        return TSelf.Succeed(valueFactory());
    }
}

/// <summary>
/// Helper that forwards collection factory calls to <see cref="ICollectionResultFactory{TSelf, TValue}"/> members.
/// </summary>
internal static class CollectionResultFactoryBridge<TSelf, TValue>
    where TSelf : struct, ICollectionResultFactory<TSelf, TValue>
{
    public static TSelf Fail(TValue[] items)
    {
        return TSelf.Fail(Problem.GenericError(), items);
    }

    public static TSelf Fail(IEnumerable<TValue> items)
    {
        var array = items as TValue[] ?? items.ToArray();
        return TSelf.Fail(Problem.GenericError(), array);
    }

    public static TSelf Fail(Problem problem, TValue[] items)
    {
        return TSelf.Fail(problem, items);
    }

    public static TSelf Fail(Problem problem, IEnumerable<TValue> items)
    {
        var array = items as TValue[] ?? items.ToArray();
        return TSelf.Fail(problem, array);
    }
}
