using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Commands.Execution;

namespace ManagedCode.Communication.Benchmark;

[MemoryDiagnoser]
[SimpleJob]
public class CommandExecutionBenchmark
{
    private static readonly Command BenchmarkCommand = Command.Create("benchmark.execute");
    private static readonly CommandExecutionRuntime Runtime = CreateRuntime();
    private static readonly CommandExecutionRuntime CircuitRuntime = CreateCircuitRuntime();
    private static readonly CommandExecutionRuntime RateLimiterRuntime = CreateRateLimiterRuntime();

    [Benchmark(Baseline = true)]
    public Result<int> DirectResult()
    {
        return Result<int>.Succeed(42);
    }

    [Benchmark]
    public ValueTask<Result<int>> NativeCommandExecution()
    {
        return CommandExecutor.ExecuteAsync(
            BenchmarkCommand,
            static (_, _) => ValueTask.FromResult(42),
            Runtime,
            CancellationToken.None);
    }

    [Benchmark]
    public ValueTask<Result<int>> ClosedCircuitCommandExecution()
    {
        return CommandExecutor.ExecuteAsync(
            BenchmarkCommand,
            static (_, _) => ValueTask.FromResult(42),
            CircuitRuntime,
            CancellationToken.None);
    }

    [Benchmark]
    public ValueTask<Result<int>> LocalRateLimitedCommandExecution()
    {
        return CommandExecutor.ExecuteAsync(
            BenchmarkCommand,
            static (_, _) => ValueTask.FromResult(42),
            RateLimiterRuntime,
            CancellationToken.None);
    }

    private static CommandExecutionRuntime CreateRuntime()
    {
        var options = new CommandExecutionOptions();
        options.Timeout.Enabled = false;
        options.Idempotency.Enabled = false;
        options.RateLimiter.Enabled = false;
        return new CommandExecutionRuntime(options);
    }

    private static CommandExecutionRuntime CreateCircuitRuntime()
    {
        var options = new CommandExecutionOptions();
        options.Timeout.Enabled = false;
        options.Idempotency.Enabled = false;
        options.RateLimiter.Enabled = false;
        options.CircuitBreaker.Enabled = true;
        options.CircuitBreaker.MinimumThroughput = int.MaxValue;
        return new CommandExecutionRuntime(options);
    }

    private static CommandExecutionRuntime CreateRateLimiterRuntime()
    {
        var options = new CommandExecutionOptions();
        options.Timeout.Enabled = false;
        options.Idempotency.Enabled = false;
        var limiter = PartitionedCommandRateLimiter.CreateConcurrency(
            static command => command.CommandType,
            permitLimit: 1);
        return new CommandExecutionRuntime(options, rateLimiter: limiter);
    }
}
