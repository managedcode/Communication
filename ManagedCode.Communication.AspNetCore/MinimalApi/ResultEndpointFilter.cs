using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ManagedCode.Communication.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using AspNetResult = Microsoft.AspNetCore.Http.IResult;
using AspNetResultFactory = System.Func<object, Microsoft.AspNetCore.Http.IResult>;
using CommunicationResult = ManagedCode.Communication.IResult;
using CommunicationResultOfObject = ManagedCode.Communication.IResult<object?>;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace ManagedCode.Communication.AspNetCore.MinimalApi;

/// <summary>
///     Endpoint filter that converts <see cref="ManagedCode.Communication.Result"/> responses into Minimal API results.
/// </summary>
public sealed class ResultEndpointFilter : IEndpointFilter
{
    // A converter can reference its result type. Cache it without extending the type's lifetime.
    private static readonly ConditionalWeakTable<Type, AspNetResultFactory> ValueResultConverters = new();

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
                : ControllerExtensions.ToProblemHttpResult(valueResult.Problem);
        }

        if (TryConvertValueResult(result, out var converted))
        {
            return converted;
        }

        if (result is CommunicationResult communicationResult)
        {
            return communicationResult.IsSuccess
                ? HttpResults.NoContent()
                : ControllerExtensions.ToProblemHttpResult(communicationResult.Problem);
        }

        return result;
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

        if (valueProperty is null || !valueProperty.CanRead)
        {
            return static result =>
            {
                var communicationResult = (CommunicationResult)result;
                return communicationResult.IsSuccess
                    ? HttpResults.NoContent()
                    : ControllerExtensions.ToProblemHttpResult(communicationResult.Problem);
            };
        }

        // Compiled once per closed Result<T> and cached, instead of paying PropertyInfo.GetValue —
        // a reflection invoke plus boxing — on every single response.
        var getValue = CompileValueGetter(type, valueProperty);

        return result =>
        {
            var communicationResult = (CommunicationResult)result;
            return communicationResult.IsSuccess
                ? HttpResults.Ok(getValue(result))
                : ControllerExtensions.ToProblemHttpResult(communicationResult.Problem);
        };
    }

    private static Func<object, object?> CompileValueGetter(Type type, PropertyInfo valueProperty)
    {
        var parameter = Expression.Parameter(typeof(object), "result");
        var typed = Expression.Convert(parameter, type);
        var access = Expression.Property(typed, valueProperty);
        var boxed = Expression.Convert(access, typeof(object));

        return Expression.Lambda<Func<object, object?>>(boxed, parameter).Compile();
    }
}
