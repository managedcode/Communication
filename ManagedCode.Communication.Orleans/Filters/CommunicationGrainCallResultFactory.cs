using System;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ManagedCode.Communication.Orleans.Helpers;
using ManagedCode.Communication.Results;
using ManagedCode.Communication.Telemetry;
using Orleans;

namespace ManagedCode.Communication.Orleans.Filters;

internal static class CommunicationGrainCallResultFactory
{
    private static readonly ConditionalWeakTable<Type, FailureFactory> FailureFactories = new();
    private static readonly FailureFactory UnsupportedReturnType = new(null);

    private static readonly MethodInfo CreateFailureGenericMethod =
        typeof(CommunicationGrainCallResultFactory).GetMethod(
            nameof(CreateFailureGeneric),
            BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new MissingMethodException(nameof(CommunicationGrainCallResultFactory), nameof(CreateFailureGeneric));

    public static bool TrySetFailure(IGrainCallContext context, Exception exception)
    {
        return TrySetFailure(context, exception, out _);
    }

    public static bool TrySetFailure(IGrainCallContext context, Exception exception, out HttpStatusCode statusCode)
    {
        var createFailure = FailureFactories.GetValue(context.InterfaceMethod.ReturnType, BuildFailureFactory).Create;
        if (createFailure is null)
        {
            statusCode = HttpStatusCode.InternalServerError;
            return false;
        }

        statusCode = OrleansHttpStatusCodeHelper.GetStatusCodeForException(exception);

        // Record the originating exception, not just the Problem derived from it: crossing a grain boundary is
        // exactly where the stack trace would otherwise be lost.
        CommunicationTelemetry.RecordFailure(Problem.Create(exception, (int)statusCode), exception);

        context.Result = createFailure(exception, statusCode);
        return true;
    }

    private static Type? GetCommunicationResultType(Type returnType)
    {
        if (!returnType.IsGenericType)
        {
            return null;
        }

        var genericDefinition = returnType.GetGenericTypeDefinition();
        if (genericDefinition != typeof(Task<>) && genericDefinition != typeof(ValueTask<>))
        {
            return null;
        }

        var resultType = returnType.GenericTypeArguments[0];
        if (!resultType.IsValueType || !typeof(IResult).IsAssignableFrom(resultType))
        {
            return null;
        }

        foreach (var contract in resultType.GetInterfaces())
        {
            if (contract.IsGenericType
                && contract.GetGenericTypeDefinition() == typeof(IResultFactory<>)
                && contract.GenericTypeArguments[0] == resultType)
            {
                return resultType;
            }
        }

        return null;
    }

    private static FailureFactory BuildFailureFactory(Type returnType)
    {
        var resultType = GetCommunicationResultType(returnType);
        return resultType is null
            ? UnsupportedReturnType
            : new FailureFactory(CreateFailureGenericMethod.MakeGenericMethod(resultType)
                .CreateDelegate<Func<Exception, HttpStatusCode, object>>());
    }

    private static object CreateFailureGeneric<TSelf>(Exception exception, HttpStatusCode statusCode)
        where TSelf : struct, IResultFactory<TSelf>
    {
        return TSelf.Fail(exception, statusCode);
    }

    private sealed class FailureFactory(Func<Exception, HttpStatusCode, object>? create)
    {
        public Func<Exception, HttpStatusCode, object>? Create { get; } = create;
    }
}
