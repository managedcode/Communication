using System;
using System.Collections.Generic;
using ManagedCode.Communication.Results.Extensions;

namespace ManagedCode.Communication;

public partial struct Result
{
    /// <summary>
    ///     Creates an exception from the result's problem.
    /// </summary>
    /// <returns>ProblemException if result has a problem, null otherwise.</returns>
    public Exception? ToException()
    {
        return ResultProblemExtensions.ToException(this);
    }

    /// <summary>
    ///     Throws a ProblemException if the result has a problem.
    /// </summary>
    public void ThrowIfProblem()
    {
        ResultProblemExtensions.ThrowIfProblem(this);
    }

    /// <summary>
    ///     Creates a UI-friendly display message for the current problem.
    /// </summary>
    public string ToDisplayMessage(string? defaultMessage = null)
    {
        return ResultProblemExtensions.ToDisplayMessage(this, defaultMessage);
    }

    /// <summary>
    ///     Creates a UI-friendly display message using an error code resolver.
    /// </summary>
    public string ToDisplayMessage(Func<string, string?> errorCodeResolver, string? defaultMessage = null)
    {
        return ResultProblemExtensions.ToDisplayMessage(this, errorCodeResolver, defaultMessage);
    }

    /// <summary>
    ///     Creates a UI-friendly display message using a dictionary map of error codes.
    /// </summary>
    public string ToDisplayMessage(IReadOnlyDictionary<string, string> messages, string? defaultMessage = null)
    {
        return ResultProblemExtensions.ToDisplayMessage(this, messages, defaultMessage);
    }

    /// <summary>
    ///     Creates a UI-friendly display message using tuple mappings.
    /// </summary>
    public string ToDisplayMessage((string code, string message) firstMapping, params (string code, string message)[] additionalMappings)
    {
        return ResultProblemExtensions.ToDisplayMessage(this, firstMapping, additionalMappings);
    }

    /// <summary>
    ///     Creates a UI-friendly display message using tuple mappings and a default message.
    /// </summary>
    public string ToDisplayMessage(
        string? defaultMessage,
        (string code, string message) firstMapping,
        params (string code, string message)[] additionalMappings)
    {
        return ResultProblemExtensions.ToDisplayMessage(this, defaultMessage, firstMapping, additionalMappings);
    }
}
