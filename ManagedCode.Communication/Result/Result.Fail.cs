using System;

namespace ManagedCode.Communication;

public partial struct Result
{
    public static Result Fail()
    {
        return new Result(false, null, default);
    }

    public static Result Fail(string message)
    {
        return new Result(false, new[] { Error.Create(message) }, default);
    }

    public static Result Fail<TEnum>(TEnum code) where TEnum : Enum
    {
        return new Result(false, new[] { Error.Create(code) }, default);
    }

    public static Result Fail<TEnum>(string message, TEnum code) where TEnum : Enum
    {
        return new Result(false, new[] { Error.Create(message, code) }, default);
    }

    public static Result Fail<TEnum>(TEnum code, string message) where TEnum : Enum
    {
        return new Result(false, new[] { Error.Create(message, code) }, default);
    }

    public static Result Fail(Error error)
    {
        return new Result(false, new[] { error }, default);
    }

    public static Result Fail(Error[]? errors)
    {
        return new Result(false, errors, default);
    }

    public static Result Fail(Exception? exception)
    {
        return new Result(false, new[] { Error.FromException(exception) }, default);
    }
}