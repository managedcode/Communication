using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.AspNetCore.Extensions;
using ManagedCode.Communication.Constants;
using Microsoft.AspNetCore.Http;
using HttpResults = Microsoft.AspNetCore.Http.Results;
using AspNetResult = Microsoft.AspNetCore.Http.IResult;
using CommunicationResult = ManagedCode.Communication.IResult;
using CommunicationResultOfObject = ManagedCode.Communication.IResult<object?>;
using AspNetResultFactory = System.Func<object, Microsoft.AspNetCore.Http.IResult>;

namespace ManagedCode.Communication.Extensions.MinimalApi;

/// <summary>
///     Endpoint filter that converts <see cref="ManagedCode.Communication.Result"/> responses into Minimal API results.
/// </summary>
public sealed class ResultEndpointFilter : IEndpointFilter
{
    private static readonly ConcurrentDictionary<Type, AspNetResultFactory> ValueResultConverters = new();

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var result = await next(context).ConfigureAwait(false);

        if (result is null)
        {
            return null;
        }

        return ConvertResult(result);
    }

    private static object ConvertResult(object result)
    {
        if (result is AspNetResult aspNetResult)
        {
            return aspNetResult;
        }

        if (result is ManagedCode.Communication.Result nonGenericResult)
        {
            return nonGenericResult.ToHttpResult();
        }

        if (result is CommunicationResultOfObject valueResult)
        {
            return valueResult.IsSuccess
                ? HttpResults.Ok(valueResult.Value)
                : CreateProblem(valueResult.Problem);
        }

        if (TryConvertValueResult(result, out var converted))
        {
            return converted;
        }

        if (result is CommunicationResult communicationResult)
        {
            return communicationResult.IsSuccess
                ? HttpResults.NoContent()
                : CreateProblem(communicationResult.Problem);
        }

        return result;
    }

    private static AspNetResult CreateProblem(Problem? problem)
    {
        var normalized = NormalizeProblem(problem);

        return HttpResults.Problem(
            title: normalized.Title,
            detail: normalized.Detail,
            statusCode: normalized.StatusCode,
            type: normalized.Type,
            instance: normalized.Instance,
            extensions: normalized.Extensions
        );
    }

    private static Problem NormalizeProblem(Problem? problem)
    {
        if (problem is null || IsGeneric(problem))
        {
            return Problem.Create("Operation failed", "Unknown error occurred", 500);
        }

        return problem;
    }

    private static bool IsGeneric(Problem problem)
    {
        return string.Equals(problem.Title, ProblemConstants.Titles.Error, StringComparison.OrdinalIgnoreCase)
               && string.Equals(problem.Detail, ProblemConstants.Messages.GenericError, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryConvertValueResult(object result, out AspNetResult converted)
    {
        converted = null!;

        var type = result.GetType();
        if (!typeof(CommunicationResult).IsAssignableFrom(type) || type == typeof(Result))
        {
            return false;
        }

        var converter = ValueResultConverters.GetOrAdd(type, CreateConverter);
        converted = converter(result);
        return true;
    }

    private static AspNetResultFactory CreateConverter(Type type)
    {
        var valueProperty = type.GetProperty("Value");

        return valueProperty is null
            ? result =>
            {
                var communicationResult = (CommunicationResult)result;
                return communicationResult.IsSuccess
                    ? HttpResults.NoContent()
                    : CreateProblem(communicationResult.Problem);
            }
            : result =>
            {
                var communicationResult = (CommunicationResult)result;
                if (communicationResult.IsSuccess)
                {
                    var value = valueProperty.GetValue(result);
                    return HttpResults.Ok(value);
                }

                return CreateProblem(communicationResult.Problem);
            };
    }
}
