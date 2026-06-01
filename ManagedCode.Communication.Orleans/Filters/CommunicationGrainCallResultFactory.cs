using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using ManagedCode.Communication.Helpers;
using ManagedCode.Communication.Results;
using Orleans;

namespace ManagedCode.Communication.Filters;

internal static class CommunicationGrainCallResultFactory
{
    private static readonly MethodInfo CreateFailureGenericMethod =
        typeof(CommunicationGrainCallResultFactory).GetMethod(
            nameof(CreateFailureGeneric),
            BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new MissingMethodException(nameof(CommunicationGrainCallResultFactory), nameof(CreateFailureGeneric));

    public static bool TrySetFailure(IGrainCallContext context, Exception exception)
    {
        var resultType = GetCommunicationResultType(context.InterfaceMethod.ReturnType);
        if (resultType is null)
        {
            return false;
        }

        var statusCode = OrleansHttpStatusCodeHelper.GetStatusCodeForException(exception);
        context.Result = CreateFailure(resultType, exception, statusCode);
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
        if (!typeof(IResult).IsAssignableFrom(resultType))
        {
            return null;
        }

        var resultFactoryType = typeof(IResultFactory<>).MakeGenericType(resultType);
        return resultFactoryType.IsAssignableFrom(resultType) ? resultType : null;
    }

    private static object CreateFailure(Type resultType, Exception exception, HttpStatusCode statusCode)
    {
        var genericMethod = CreateFailureGenericMethod.MakeGenericMethod(resultType);
        return genericMethod.Invoke(obj: null, [exception, statusCode])
            ?? throw new InvalidOperationException(resultType.FullName);
    }

    private static TSelf CreateFailureGeneric<TSelf>(Exception exception, HttpStatusCode statusCode)
        where TSelf : struct, IResultFactory<TSelf>
    {
        return TSelf.Fail(exception, statusCode);
    }
}
