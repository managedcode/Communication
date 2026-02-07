using System;
using System.Collections.Generic;
using ManagedCode.Communication.Constants;
using ManagedCode.Communication;

namespace ManagedCode.Communication.Results.Extensions;

/// <summary>
///     Problem-related helpers for <see cref="IResultProblem"/> implementations.
/// </summary>
public static class ResultProblemExtensions
{
    public static Exception? ToException(this IResultProblem result)
    {
        return result.Problem is not null ? new ProblemException(result.Problem) : null;
    }

    public static void ThrowIfProblem(this IResultProblem result)
    {
        if (result.Problem is not null)
        {
            throw new ProblemException(result.Problem);
        }
    }

    public static string ToDisplayMessage(this IResultProblem result, string? defaultMessage = null)
    {
        var problem = result.TryGetProblem(out var extractedProblem)
            ? extractedProblem
            : result.Problem;

        if (problem is not null)
        {
            return problem.ToDisplayMessage(defaultMessage);
        }

        return !string.IsNullOrWhiteSpace(defaultMessage) ? defaultMessage : ProblemConstants.Messages.GenericError;
    }

    public static string ToDisplayMessage(
        this IResultProblem result,
        Func<string, string?> errorCodeResolver,
        string? defaultMessage = null)
    {
        ArgumentNullException.ThrowIfNull(errorCodeResolver);

        var problem = result.TryGetProblem(out var extractedProblem)
            ? extractedProblem
            : result.Problem;

        if (problem is not null)
        {
            return problem.ToDisplayMessage(errorCodeResolver, defaultMessage);
        }

        return !string.IsNullOrWhiteSpace(defaultMessage) ? defaultMessage : ProblemConstants.Messages.GenericError;
    }

    public static string ToDisplayMessage(
        this IResultProblem result,
        IReadOnlyDictionary<string, string> messages,
        string? defaultMessage = null)
    {
        ArgumentNullException.ThrowIfNull(messages);
        return ToDisplayMessage(result, problem => messages.TryGetValue(problem, out var message) ? message : null, defaultMessage);
    }

    public static string ToDisplayMessage(
        this IResultProblem result,
        IEnumerable<KeyValuePair<string, string>> messages,
        string? defaultMessage = null)
    {
        ArgumentNullException.ThrowIfNull(messages);
        return ToDisplayMessage(result, BuildMessageMap(messages), defaultMessage);
    }

    public static string ToDisplayMessage(
        this IResultProblem result,
        (string code, string message) firstMapping,
        params (string code, string message)[] additionalMappings)
    {
        return ToDisplayMessage(result, defaultMessage: null, firstMapping, additionalMappings);
    }

    public static string ToDisplayMessage(
        this IResultProblem result,
        string? defaultMessage,
        (string code, string message) firstMapping,
        params (string code, string message)[] additionalMappings)
    {
        var mappings = new List<(string code, string message)>(1 + additionalMappings.Length) { firstMapping };
        mappings.AddRange(additionalMappings);
        return ToDisplayMessage(result, BuildMessageMap(mappings), defaultMessage);
    }

    private static IReadOnlyDictionary<string, string> BuildMessageMap(IEnumerable<KeyValuePair<string, string>> messages)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var mapping in messages)
        {
            if (string.IsNullOrWhiteSpace(mapping.Key) || string.IsNullOrWhiteSpace(mapping.Value))
            {
                continue;
            }

            map[mapping.Key] = mapping.Value;
        }

        return map;
    }

    private static IReadOnlyDictionary<string, string> BuildMessageMap(IEnumerable<(string code, string message)> mappings)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (code, message) in mappings)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(message))
            {
                continue;
            }

            map[code] = message;
        }

        return map;
    }
}
