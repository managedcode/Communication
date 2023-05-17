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
    public object? ActivatorCreateInstanceTypeErrorInterface()
    {
        var result = Activator.CreateInstance(typeof(Result));
        (result as IResultError).AddError(Error.Create("oops"));
        return result;
    }

    [Benchmark]
    public object? ActivatorCreateInstanceTypeIntErrorInterface()
    {
        var result = Activator.CreateInstance(typeof(Result<int>));
        (result as IResultError).AddError(Error.Create("oops"));
        return result;
    }
}