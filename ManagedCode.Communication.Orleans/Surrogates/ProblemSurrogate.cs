using System.Collections.Generic;
using Orleans;

namespace ManagedCode.Communication.Orleans.Surrogates;

/// <summary>
///     Orleans serialization surrogate for <c>Problem</c>.
/// </summary>
[Immutable]
[GenerateSerializer]
public struct ProblemSurrogate
{
    /// <summary>
    ///     Creates the surrogate from its parts.
    /// </summary>
    public ProblemSurrogate(string? type, string? title, int statusCode, string? detail, string? instance, IDictionary<string, object?> extensions)
    {
        Type = type;
        Title = title;
        StatusCode = statusCode;
        Detail = detail;
        Instance = instance;
        Extensions = extensions;
    }

    /// <summary>
    ///     RFC 7807 problem type URI.
    /// </summary>
    [Id(0)] public string? Type;
    /// <summary>
    ///     RFC 7807 problem title.
    /// </summary>
    [Id(1)] public string? Title;
    /// <summary>
    ///     HTTP status code.
    /// </summary>
    [Id(2)] public int StatusCode;
    /// <summary>
    ///     RFC 7807 problem detail.
    /// </summary>
    [Id(3)] public string? Detail;
    /// <summary>
    ///     RFC 7807 problem instance.
    /// </summary>
    [Id(4)] public string? Instance;
    /// <summary>
    ///     Extension data.
    /// </summary>
    [Id(5)] public IDictionary<string, object?> Extensions;
}