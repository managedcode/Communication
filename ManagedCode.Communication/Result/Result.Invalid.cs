using System;
using System.Collections.Generic;
using System.Linq;

namespace ManagedCode.Communication;

public partial struct Result
{
    public static Result Invalid()
    {
        return FailValidation(("message", nameof(Invalid)));
    }

    public static Result Invalid<TEnum>(TEnum code) where TEnum : Enum
    {
        var problem = Problem.Validation(("message", nameof(Invalid)));
        problem.ErrorCode = code.ToString();
        return new Result(false, problem);
    }

    public static Result Invalid(string message)
    {
        return FailValidation((nameof(message), message));
    }

    public static Result Invalid<TEnum>(TEnum code, string message) where TEnum : Enum
    {
        var problem = Problem.Validation((nameof(message), message));
        problem.ErrorCode = code.ToString();
        return new Result(false, problem);
    }

    public static Result Invalid(string key, string value)
    {
        return FailValidation((key, value));
    }

    public static Result Invalid<TEnum>(TEnum code, string key, string value) where TEnum : Enum
    {
        var problem = Problem.Validation((key, value));
        problem.ErrorCode = code.ToString();
        return new Result(false, problem);
    }

    public static Result Invalid(Dictionary<string, string> values)
    {
        return FailValidation(values.Select(kvp => (kvp.Key, kvp.Value))
            .ToArray());
    }

    public static Result Invalid<TEnum>(TEnum code, Dictionary<string, string> values) where TEnum : Enum
    {
        var problem = Problem.Validation(values.Select(kvp => (kvp.Key, kvp.Value))
            .ToArray());
        problem.ErrorCode = code.ToString();
        return new Result(false, problem);
    }


    public static Result<T> Invalid<T>()
    {
        return Result<T>.FailValidation(("message", nameof(Invalid)));
    }

    public static Result<T> Invalid<T, TEnum>(TEnum code) where TEnum : Enum
    {
        var problem = Problem.Validation(("message", nameof(Invalid)));
        problem.ErrorCode = code.ToString();
        return Result<T>.Fail(problem);
    }

    public static Result<T> Invalid<T>(string message)
    {
        return Result<T>.FailValidation((nameof(message), message));
    }

    public static Result<T> Invalid<T, TEnum>(TEnum code, string message) where TEnum : Enum
    {
        var problem = Problem.Validation((nameof(message), message));
        problem.ErrorCode = code.ToString();
        return Result<T>.Fail(problem);
    }

    public static Result<T> Invalid<T>(string key, string value)
    {
        return Result<T>.FailValidation((key, value));
    }

    public static Result<T> Invalid<T, TEnum>(TEnum code, string key, string value) where TEnum : Enum
    {
        var problem = Problem.Validation((key, value));
        problem.ErrorCode = code.ToString();
        return Result<T>.Fail(problem);
    }

    public static Result<T> Invalid<T>(Dictionary<string, string> values)
    {
        return Result<T>.FailValidation(values.Select(kvp => (kvp.Key, kvp.Value))
            .ToArray());
    }

    public static Result<T> Invalid<T, TEnum>(TEnum code, Dictionary<string, string> values) where TEnum : Enum
    {
        var problem = Problem.Validation(values.Select(kvp => (kvp.Key, kvp.Value))
            .ToArray());
        problem.ErrorCode = code.ToString();
        return Result<T>.Fail(problem);
    }
}