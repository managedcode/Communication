using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;

namespace ManagedCode.Communication.Benchmark;


[SimpleJob]
public class CreateInstanceFailBenchmark
{
    
    [Benchmark(Baseline = true)]
    public Result ResultFail() => Result.Fail();
    
    [Benchmark]
    public Result ResultFailMessage() => Result.Fail("oops");
    
    [Benchmark]
    public Result ResultFailInt() => Result.Fail<int>();
    
    [Benchmark]
    public Result ResultFailIntMessage() => Result.Fail<int>("oops");
    
    [Benchmark]
    public Result ActivatorCreateInstanceGeneric() => Activator.CreateInstance<Result>();
    
    [Benchmark]
    public Result ActivatorCreateInstanceGenericInt() => Activator.CreateInstance<Result<int>>();
    
    [Benchmark]
    public object? ActivatorCreateInstanceType() => Activator.CreateInstance(typeof(Result));
    
    [Benchmark]
    public object? ActivatorCreateInstanceTypeInt() => Activator.CreateInstance(typeof(Result<int>));

    
    [Benchmark]
    public object? ActivatorCreateInstanceTypeError()
    {
        var result = (Result)Activator.CreateInstance(typeof(Result));
        result.Errors = new[] { Error.Create("oops") };
        return result;
    }
    
    [Benchmark]
    public object? ActivatorCreateInstanceTypeIntError()
    {
        var result = (Result<int>)Activator.CreateInstance(typeof(Result<int>));
        result.Errors = new[] { Error.Create("oops") };
        return result;
    }
    
    [Benchmark]
    public object? ActivatorCreateInstanceCtorTypeError()
    {
        return Activator.CreateInstance(typeof(Result), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] {Error.Create("oops")}, null);;
    }

    
    [Benchmark]
    public object? ActivatorCreateInstanceCtorTypeIntError()
    {
        return Activator.CreateInstance(typeof(Result<int>), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] {Error.Create("oops")}, null);;
    }
    
}




