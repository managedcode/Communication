using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace ManagedCode.Communication.Benchmark;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class ResultCreationBenchmark
{
    private static readonly Exception TestException = new InvalidOperationException("Benchmark test");
    private static readonly Type ResultIntType = typeof(Result<int>);
    private static readonly ConcurrentDictionary<Type, MethodInfo> MethodCache = new();
    
    private MethodInfo _cachedMethod = null!;
    private readonly object[] _exceptionArray = new object[1];
    private readonly Exception _exception = TestException;
    
    [GlobalSetup]
    public void Setup()
    {
        _cachedMethod = ResultIntType.GetMethod(nameof(Result.Fail), [typeof(Exception), typeof(HttpStatusCode)])!;
        MethodCache[ResultIntType] = _cachedMethod;
        _exceptionArray[0] = TestException;
    }

    [Benchmark(Baseline = true)]
    public Result<int> DirectCall()
    {
        return Result<int>.Fail(TestException);
    }

    [Benchmark]
    public object Reflection_FindMethodEveryTime()
    {
        var method = ResultIntType.GetMethod(nameof(Result.Fail), [typeof(Exception), typeof(HttpStatusCode)]);
        return method!.Invoke(null, [TestException, HttpStatusCode.InternalServerError])!;
    }

    [Benchmark]
    public object Reflection_CachedMethod()
    {
        return _cachedMethod.Invoke(null, [TestException, HttpStatusCode.InternalServerError])!;
    }

    [Benchmark]
    public object Reflection_CachedMethod_ReuseArray()
    {
        // Can't reuse array because we need 2 parameters
        return _cachedMethod.Invoke(null, [TestException, HttpStatusCode.InternalServerError])!;
    }

    [Benchmark]
    public object Reflection_ConcurrentDictionary()
    {
        var method = MethodCache.GetOrAdd(ResultIntType, type => 
            type.GetMethod(nameof(Result.Fail), [typeof(Exception), typeof(HttpStatusCode)])!);
        return method.Invoke(null, [TestException, HttpStatusCode.InternalServerError])!;
    }

    [Benchmark]
    public object Activator_TryCreateInstance()
    {
        var result = Activator.CreateInstance(ResultIntType);
        return result!;
    }
    
    [Benchmark]
    public object Activator_WithPropertySet()
    {
        var resultType = Activator.CreateInstance(ResultIntType, 
            BindingFlags.NonPublic | BindingFlags.Instance, 
            null, [TestException], 
            CultureInfo.CurrentCulture);
        
        return resultType!;
    }
}