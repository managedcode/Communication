using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.AspNetCore.Filters;
using ManagedCode.Communication.AspNetCore.MinimalApi;
using ManagedCode.Communication.CQRS;
using Microsoft.AspNetCore.Http;
using Shouldly;
using AspNetResult = Microsoft.AspNetCore.Http.IResult;

namespace ManagedCode.Communication.Tests.AspNetCore.Extensions;

[NotInParallel]
public class CollectibleResultCacheTests
{
    private const string PayloadAssemblyName = "CollectibleResultPayload";
    private const string ModuleName = "Main";
    private const string PayloadName = "Payload";
    private const string PayloadMemberName = "Ready";

    [Test]
    public void MinimalApiResultConverter_AllowsPayloadTypeCollection()
    {
        AssertCollected(InvokeMinimalApiFilter());
    }

    [Test]
    public void CqrsConverter_AllowsStreamTypeCollection()
    {
        AssertCollected(InvokeCqrsFilter(isStream: true));
    }

    [Test]
    public void CqrsNegativeCache_AllowsPlainTypeCollection()
    {
        AssertCollected(InvokeCqrsFilter(isStream: false));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference InvokeMinimalApiFilter()
    {
        var payloadType = CreateCollectiblePayloadType();
        var payload = Enum.ToObject(payloadType, 1);
        var resultType = typeof(Result<>).MakeGenericType(payloadType);
        var factory = resultType.GetMethod(nameof(Result<int>.Succeed), [payloadType])!;
        var result = factory.Invoke(null, [payload]);
        var converted = Invoke(new ResultEndpointFilter(), result);

        converted.ShouldBeAssignableTo<IValueHttpResult>()!.Value.ShouldBe(payload);
        return new WeakReference(resultType);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference InvokeCqrsFilter(bool isStream)
    {
        var payloadType = CreateCollectiblePayloadType();
        var resultType = isStream ? typeof(EmptyStream<>).MakeGenericType(payloadType) : payloadType;
        var result = Activator.CreateInstance(resultType, nonPublic: true);
        var converted = Invoke(new CqrsResultEndpointFilter(), result);

        if (isStream)
        {
            converted.ShouldBeAssignableTo<AspNetResult>();
        }
        else
        {
            converted.ShouldBeSameAs(result);
        }

        return new WeakReference(resultType);
    }

    private static object? Invoke(IEndpointFilter filter, object? result)
    {
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext());
        return filter.InvokeAsync(context, _ => ValueTask.FromResult(result)).GetAwaiter().GetResult();
    }

    private static Type CreateCollectiblePayloadType()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(PayloadAssemblyName), AssemblyBuilderAccess.RunAndCollect);
        var module = assembly.DefineDynamicModule(ModuleName);
        var type = module.DefineEnum(PayloadName, TypeAttributes.Public, typeof(int));
        type.DefineLiteral(PayloadMemberName, 1);
        return type.CreateTypeInfo().AsType();
    }

    private static void AssertCollected(WeakReference type)
    {
        for (var i = 0; i < 10 && type.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        type.IsAlive.ShouldBeFalse();
    }

    private sealed class EmptyStream<T> : IAsyncEnumerable<CqrsStreamChunk<int, T>>
    {
        public IAsyncEnumerator<CqrsStreamChunk<int, T>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return AsyncEnumerable.Empty<CqrsStreamChunk<int, T>>().GetAsyncEnumerator(cancellationToken);
        }
    }
}
