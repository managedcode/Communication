using System.Collections.Generic;
using Orleans;

namespace ManagedCode.Communication.Surrogates;

[Immutable]
[GenerateSerializer]
public struct ProblemSurrogate
{
    public ProblemSurrogate(string? type, string? title, int statusCode, string? detail, string? instance, IDictionary<string, object?> extensions)
    {
        Type = type;
        Title = title;
        StatusCode = statusCode;
        Detail = detail;
        Instance = instance;
        Extensions = extensions;
    }

    [Id(0)] public string? Type;
    [Id(1)] public string? Title;
    [Id(2)] public int StatusCode;
    [Id(3)] public string? Detail;
    [Id(4)] public string? Instance;
    [Id(5)] public IDictionary<string, object?> Extensions;
}