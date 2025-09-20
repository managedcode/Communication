using System;
using System.Net;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication.Results;

public partial interface IResultFactory<TSelf>
    where TSelf : struct, IResultFactory<TSelf>
{
    static virtual TSelf Fail()
    {
        return TSelf.Fail(Problem.GenericError());
    }

    static virtual TSelf Fail(string title)
    {
        return TSelf.Fail(Problem.Create(title, title, HttpStatusCode.InternalServerError));
    }

    static virtual TSelf Fail(string title, string detail)
    {
        return TSelf.Fail(Problem.Create(title, detail));
    }

    static virtual TSelf Fail(string title, string detail, HttpStatusCode status)
    {
        return TSelf.Fail(Problem.Create(title, detail, (int)status));
    }

    static virtual TSelf Fail(Exception exception)
    {
        return TSelf.Fail(Problem.Create(exception, (int)HttpStatusCode.InternalServerError));
    }

    static virtual TSelf Fail(Exception exception, HttpStatusCode status)
    {
        return TSelf.Fail(Problem.Create(exception, (int)status));
    }

    static virtual TSelf Fail<TEnum>(TEnum errorCode) where TEnum : Enum
    {
        return TSelf.Fail(Problem.Create(errorCode));
    }

    static virtual TSelf Fail<TEnum>(TEnum errorCode, string detail) where TEnum : Enum
    {
        return TSelf.Fail(Problem.Create(errorCode, detail));
    }

    static virtual TSelf Fail<TEnum>(TEnum errorCode, HttpStatusCode status) where TEnum : Enum
    {
        return TSelf.Fail(Problem.Create(errorCode, errorCode.ToString(), (int)status));
    }

    static virtual TSelf Fail<TEnum>(TEnum errorCode, string detail, HttpStatusCode status) where TEnum : Enum
    {
        return TSelf.Fail(Problem.Create(errorCode, detail, (int)status));
    }
}
