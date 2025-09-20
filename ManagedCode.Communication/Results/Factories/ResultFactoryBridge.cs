using System;
using System.Collections.Generic;
using System.Net;
using ManagedCode.Communication;

namespace ManagedCode.Communication.Results;

internal static class ResultFactoryBridge<TSelf>
    where TSelf : struct, IResultFactory<TSelf>
{
    public static TSelf Succeed() => TSelf.Succeed();

    public static TSelf Fail() => IResultFactory<TSelf>.Fail();

    public static TSelf Fail(Problem problem) => TSelf.Fail(problem);

    public static TSelf Fail(string title) => IResultFactory<TSelf>.Fail(title);

    public static TSelf Fail(string title, string detail) => IResultFactory<TSelf>.Fail(title, detail);

    public static TSelf Fail(string title, string detail, HttpStatusCode status) => IResultFactory<TSelf>.Fail(title, detail, status);

    public static TSelf Fail(Exception exception) => IResultFactory<TSelf>.Fail(exception);

    public static TSelf Fail(Exception exception, HttpStatusCode status) => IResultFactory<TSelf>.Fail(exception, status);

    public static TSelf Fail<TEnum>(TEnum errorCode) where TEnum : Enum => IResultFactory<TSelf>.Fail(errorCode);

    public static TSelf Fail<TEnum>(TEnum errorCode, string detail) where TEnum : Enum => IResultFactory<TSelf>.Fail(errorCode, detail);

    public static TSelf Fail<TEnum>(TEnum errorCode, HttpStatusCode status) where TEnum : Enum => IResultFactory<TSelf>.Fail(errorCode, status);

    public static TSelf Fail<TEnum>(TEnum errorCode, string detail, HttpStatusCode status) where TEnum : Enum
    {
        return IResultFactory<TSelf>.Fail(errorCode, detail, status);
    }

    public static TSelf FailBadRequest(string? detail = null) => IResultFactory<TSelf>.FailBadRequest(detail);

    public static TSelf FailUnauthorized(string? detail = null) => IResultFactory<TSelf>.FailUnauthorized(detail);

    public static TSelf FailForbidden(string? detail = null) => IResultFactory<TSelf>.FailForbidden(detail);

    public static TSelf FailNotFound(string? detail = null) => IResultFactory<TSelf>.FailNotFound(detail);

    public static TSelf FailValidation(params (string field, string message)[] errors)
    {
        return IResultFactory<TSelf>.FailValidation(errors);
    }

    public static TSelf Invalid() => IResultFactory<TSelf>.Invalid();

    public static TSelf Invalid<TEnum>(TEnum code) where TEnum : Enum => IResultFactory<TSelf>.Invalid(code);

    public static TSelf Invalid(string message) => IResultFactory<TSelf>.Invalid(message);

    public static TSelf Invalid<TEnum>(TEnum code, string message) where TEnum : Enum
    {
        return IResultFactory<TSelf>.Invalid(code, message);
    }

    public static TSelf Invalid(string key, string value) => IResultFactory<TSelf>.Invalid(key, value);

    public static TSelf Invalid<TEnum>(TEnum code, string key, string value) where TEnum : Enum
    {
        return IResultFactory<TSelf>.Invalid(code, key, value);
    }

    public static TSelf Invalid(IEnumerable<KeyValuePair<string, string>> values)
    {
        return IResultFactory<TSelf>.Invalid(values);
    }

    public static TSelf Invalid<TEnum>(TEnum code, IEnumerable<KeyValuePair<string, string>> values) where TEnum : Enum
    {
        return IResultFactory<TSelf>.Invalid(code, values);
    }
}

internal static class ResultValueFactoryBridge<TSelf, TValue>
    where TSelf : struct, IResultValueFactory<TSelf, TValue>
{
    public static TSelf Succeed(TValue value) => TSelf.Succeed(value);

    public static TSelf Succeed(Func<TValue> valueFactory)
    {
        return IResultValueFactory<TSelf, TValue>.Succeed(valueFactory);
    }
}

internal static class CollectionResultFactoryBridge<TSelf, TValue>
    where TSelf : struct, ICollectionResultFactory<TSelf, TValue>
{
    public static TSelf Fail(TValue[] items) => ICollectionResultFactory<TSelf, TValue>.Fail(items);

    public static TSelf Fail(IEnumerable<TValue> items)
    {
        return ICollectionResultFactory<TSelf, TValue>.Fail(items);
    }

    public static TSelf Fail(Problem problem, TValue[] items)
    {
        return TSelf.Fail(problem, items);
    }

    public static TSelf Fail(Problem problem, IEnumerable<TValue> items)
    {
        return ICollectionResultFactory<TSelf, TValue>.Fail(problem, items);
    }
}
