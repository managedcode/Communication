using System;
using System.Collections.Generic;
using System.Linq;
using ManagedCode.Communication;

namespace ManagedCode.Communication.Results;

public partial interface IResultFactory<TSelf>
    where TSelf : struct, IResultFactory<TSelf>
{
    static virtual TSelf Invalid()
    {
        return TSelf.FailValidation(("message", nameof(Invalid)));
    }

    static virtual TSelf Invalid<TEnum>(TEnum code) where TEnum : Enum
    {
        return Invalid(code, ("message", nameof(Invalid)));
    }

    static virtual TSelf Invalid(string message)
    {
        return TSelf.FailValidation((nameof(message), message));
    }

    static virtual TSelf Invalid<TEnum>(TEnum code, string message) where TEnum : Enum
    {
        return Invalid(code, (nameof(message), message));
    }

    static virtual TSelf Invalid(string key, string value)
    {
        return TSelf.FailValidation((key, value));
    }

    static virtual TSelf Invalid<TEnum>(TEnum code, string key, string value) where TEnum : Enum
    {
        return Invalid(code, (key, value));
    }

    static virtual TSelf Invalid(IEnumerable<KeyValuePair<string, string>> values)
    {
        var entries = values?.Select(pair => (pair.Key, pair.Value)).ToArray()
                      ?? Array.Empty<(string field, string message)>();
        return TSelf.FailValidation(entries);
    }

    static virtual TSelf Invalid<TEnum>(TEnum code, IEnumerable<KeyValuePair<string, string>> values) where TEnum : Enum
    {
        var entries = values?.Select(pair => (pair.Key, pair.Value)).ToArray()
                      ?? Array.Empty<(string field, string message)>();
        var problem = Problem.Validation(entries);
        problem.ErrorCode = code.ToString();
        return TSelf.Fail(problem);
    }

    private static TSelf Invalid<TEnum>(TEnum code, (string field, string message) entry) where TEnum : Enum
    {
        var problem = Problem.Validation(new[] { entry });
        problem.ErrorCode = code.ToString();
        return TSelf.Fail(problem);
    }
}
