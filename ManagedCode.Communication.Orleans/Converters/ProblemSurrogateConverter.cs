using ManagedCode.Communication.Constants;
using ManagedCode.Communication.Surrogates;
using Orleans;

namespace ManagedCode.Communication.Converters;

[RegisterConverter]
public sealed class ProblemSurrogateConverter : IConverter<Problem, ProblemSurrogate>
{
    public Problem ConvertFromSurrogate(in ProblemSurrogate surrogate)
    {
        var problem = surrogate.Instance != null && surrogate.Type != null
            ? Problem.Create(
                surrogate.Title ?? ProblemConstants.Titles.Error, 
                surrogate.Detail ?? ProblemConstants.Messages.GenericError, 
                surrogate.StatusCode, 
                surrogate.Type, 
                surrogate.Instance)
            : surrogate.Type != null
                ? Problem.Create(
                    surrogate.Title ?? ProblemConstants.Titles.Error, 
                    surrogate.Detail ?? ProblemConstants.Messages.GenericError, 
                    surrogate.StatusCode, 
                    surrogate.Type)
                : Problem.Create(
                    surrogate.Title ?? ProblemConstants.Titles.Error, 
                    surrogate.Detail ?? ProblemConstants.Messages.GenericError, 
                    surrogate.StatusCode);

        // Copy extensions using the WithExtensions method
        if (surrogate.Extensions.Count > 0)
        {
            return problem.WithExtensions(surrogate.Extensions);
        }

        return problem;
    }

    public ProblemSurrogate ConvertToSurrogate(in Problem value)
    {
        return new ProblemSurrogate(value.Type, value.Title, value.StatusCode, value.Detail, value.Instance, value.Extensions);
    }
}