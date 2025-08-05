using System;
using System.Collections.Generic;
using System.Linq;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    public static Result<T> Invalid()
    {
        return FailValidation(("message", nameof(Invalid)));
    }
    
    public static Result<T> Invalid<TEnum>(TEnum code) where TEnum : Enum
    {
        var problem = Problem.Validation(("message", nameof(Invalid)));
        problem.ErrorCode = code.ToString();
        return Fail(problem);
    }

    public static Result<T> Invalid(string message)
    {
        return FailValidation((nameof(message), message));
    }
    
    public static Result<T> Invalid<TEnum>(TEnum code, string message) where TEnum : Enum
    {
        var problem = Problem.Validation((nameof(message), message));
        problem.ErrorCode = code.ToString();
        return Fail(problem);
    }

    public static Result<T> Invalid(string key, string value)
    {
        return FailValidation((key, value));
    }
    
    public static Result<T> Invalid<TEnum>(TEnum code, string key, string value) where TEnum : Enum
    {
        var problem = Problem.Validation((key, value));
        problem.ErrorCode = code.ToString();
        return Fail(problem);
    }

    public static Result<T> Invalid(Dictionary<string, string> values)
    {
        return FailValidation(values.Select(kvp => (kvp.Key, kvp.Value)).ToArray());
    }
    
    public static Result<T> Invalid<TEnum>(TEnum code, Dictionary<string, string> values) where TEnum : Enum
    {
        var problem = Problem.Validation(values.Select(kvp => (kvp.Key, kvp.Value)).ToArray());
        problem.ErrorCode = code.ToString();
        return Fail(problem);
    }
}