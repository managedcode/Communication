using System.Net;
using BenchmarkDotNet.Attributes;

namespace ManagedCode.Communication.Benchmark;

[SimpleJob]
public class CreateInstanceFailBenchmark
{
    [Benchmark(Baseline = true)]
    public Result ResultFail()
    {
        return Result.Fail();
    }

    [Benchmark]
    public Result ResultFailMessage()
    {
        return Result.Fail("oops");
    }

    [Benchmark]
    public Result ResultFailInt()
    {
        return Result.Fail<int>();
    }

    [Benchmark]
    public Result ResultFailIntMessage()
    {
        return Result.Fail<int>("oops");
    }

    [Benchmark]
    public Result ActivatorCreateInstanceGeneric()
    {
        return Activator.CreateInstance<Result>();
    }

    [Benchmark]
    public Result ActivatorCreateInstanceGenericInt()
    {
        return Activator.CreateInstance<Result<int>>();
    }

    [Benchmark]
    public object? ActivatorCreateInstanceType()
    {
        return Activator.CreateInstance(typeof(Result));
    }

    [Benchmark]
    public object? ActivatorCreateInstanceTypeInt()
    {
        return Activator.CreateInstance(typeof(Result<int>));
    }


    [Benchmark]
    public Result CreateInstanceTypeError()
    {
        return Result.Fail("about:blank", "Error", HttpStatusCode.BadRequest);
    }

    [Benchmark]
    public Result<int> CreateInstanceTypeIntError()
    {
        return Result<int>.Fail("about:blank", "Error", HttpStatusCode.BadRequest);
    }

    [Benchmark]
    public Result CreateInstanceTypeErrorInterface()
    {
        return Result.Fail("about:blank", "Error", HttpStatusCode.BadRequest);
    }

    [Benchmark]
    public Result<int> CreateInstanceTypeIntErrorInterface()
    {
        return Result<int>.Fail("about:blank", "Error", HttpStatusCode.BadRequest);
    }
}