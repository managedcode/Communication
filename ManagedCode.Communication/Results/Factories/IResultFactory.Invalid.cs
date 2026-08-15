using System;
using System.Collections.Generic;
using System.Linq;
using ManagedCode.Communication;

namespace ManagedCode.Communication.Results;

public partial interface IResultFactory<TSelf>
    where TSelf : struct, IResultFactory<TSelf>
{
    /// <summary>
    ///     Creates a validation failure with no field details.
    /// </summary>
    static virtual TSelf Invalid()
    {
        return TSelf.FailValidation(("message", nameof(Invalid)));
    }

    /// <summary>
    ///     Creates a validation failure identified by an enum error code.
    /// </summary>
    static virtual TSelf Invalid<TEnum>(TEnum code) where TEnum : Enum
    {
        return Invalid(code, ("message", nameof(Invalid)));
    }

    /// <summary>
    ///     Creates a validation failure with a general message.
    /// </summary>
    static virtual TSelf Invalid(string message)
    {
        return TSelf.FailValidation((nameof(message), message));
    }

    /// <summary>
    ///     Creates a validation failure with an enum error code and a general message.
    /// </summary>
    static virtual TSelf Invalid<TEnum>(TEnum code, string message) where TEnum : Enum
    {
        return Invalid(code, (nameof(message), message));
    }

    /// <summary>
    ///     Creates a validation failure for a single field.
    /// </summary>
    static virtual TSelf Invalid(string key, string value)
    {
        return TSelf.FailValidation((key, value));
    }

    /// <summary>
    ///     Creates a validation failure for a single field, identified by an enum error code.
    /// </summary>
    static virtual TSelf Invalid<TEnum>(TEnum code, string key, string value) where TEnum : Enum
    {
        return Invalid(code, (key, value));
    }

    /// <summary>
    ///     Creates a validation failure from field/message pairs.
    /// </summary>
    static virtual TSelf Invalid(IEnumerable<KeyValuePair<string, string>> values)
    {
        var entries = values?.Select(pair => (pair.Key, pair.Value)).ToArray()
                      ?? Array.Empty<(string field, string message)>();
        return TSelf.FailValidation(entries);
    }

    /// <summary>
    ///     Creates a validation failure from field/message pairs, identified by an enum error code.
    /// </summary>
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
