using System;
using System.Net;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication.Results;

public partial interface IResultFactory<TSelf>
    where TSelf : struct, IResultFactory<TSelf>
{
    /// <summary>
    ///     Creates a failure with the generic fallback problem.
    /// </summary>
    static virtual TSelf Fail()
    {
        return TSelf.Fail(Problem.GenericError());
    }

    /// <summary>
    ///     Creates a failure with the given title and a 500 status.
    /// </summary>
    static virtual TSelf Fail(string title)
    {
        return TSelf.Fail(Problem.Create(title, title, HttpStatusCode.InternalServerError));
    }

    /// <summary>
    ///     Creates a failure with the given title and detail.
    /// </summary>
    static virtual TSelf Fail(string title, string detail)
    {
        return TSelf.Fail(Problem.Create(title, detail));
    }

    /// <summary>
    ///     Creates a failure with the given title, detail and status code.
    /// </summary>
    static virtual TSelf Fail(string title, string detail, HttpStatusCode status)
    {
        return TSelf.Fail(Problem.Create(title, detail, (int)status));
    }

    /// <summary>
    ///     Creates a failure from an exception.
    /// </summary>
    static virtual TSelf Fail(Exception exception)
    {
        return TSelf.Fail(Problem.Create(exception, (int)HttpStatusCode.InternalServerError));
    }

    /// <summary>
    ///     Creates a failure from an exception with an explicit status code.
    /// </summary>
    static virtual TSelf Fail(Exception exception, HttpStatusCode status)
    {
        return TSelf.Fail(Problem.Create(exception, (int)status));
    }

    /// <summary>
    ///     Creates a failure identified by an enum error code.
    /// </summary>
    static virtual TSelf Fail<TEnum>(TEnum errorCode) where TEnum : Enum
    {
        return TSelf.Fail(Problem.Create(errorCode));
    }

    /// <summary>
    ///     Creates a failure identified by an enum error code, with a detail message.
    /// </summary>
    static virtual TSelf Fail<TEnum>(TEnum errorCode, string detail) where TEnum : Enum
    {
        return TSelf.Fail(Problem.Create(errorCode, detail));
    }

    /// <summary>
    ///     Creates a failure identified by an enum error code, with an explicit status code.
    /// </summary>
    static virtual TSelf Fail<TEnum>(TEnum errorCode, HttpStatusCode status) where TEnum : Enum
    {
        return TSelf.Fail(Problem.Create(errorCode, errorCode.ToString(), (int)status));
    }

    /// <summary>
    ///     Creates a failure identified by an enum error code, with a detail message and status code.
    /// </summary>
    static virtual TSelf Fail<TEnum>(TEnum errorCode, string detail, HttpStatusCode status) where TEnum : Enum
    {
        return TSelf.Fail(Problem.Create(errorCode, detail, (int)status));
    }
}
