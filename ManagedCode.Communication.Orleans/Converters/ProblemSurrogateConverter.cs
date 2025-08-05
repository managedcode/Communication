using System.Collections.Generic;
using ManagedCode.Communication.Surrogates;
using Orleans;

namespace ManagedCode.Communication.Converters;

[RegisterConverter]
public sealed class ProblemSurrogateConverter : IConverter<Problem, ProblemSurrogate>
{
    public Problem ConvertFromSurrogate(in ProblemSurrogate surrogate)
    {
        var problem = Problem.Create(
            surrogate.Type ?? "about:blank",
            surrogate.Title ?? "Error",
            surrogate.StatusCode,
            surrogate.Detail ?? "An error occurred",
            surrogate.Instance);
        
        // Copy extensions using the WithExtensions method
        if (surrogate.Extensions.Count > 0)
        {
            return problem.WithExtensions(surrogate.Extensions);
        }
        
        return problem;
    }

    public ProblemSurrogate ConvertToSurrogate(in Problem value)
    {
        return new ProblemSurrogate(
            value.Type,
            value.Title,
            value.StatusCode,
            value.Detail,
            value.Instance,
            value.Extensions);
    }
}