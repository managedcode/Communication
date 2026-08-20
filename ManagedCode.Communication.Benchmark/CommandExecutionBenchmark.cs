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

    private static CommandExecutionRuntime CreateRuntime()
    {
        var options = new CommandExecutionOptions();
        options.Timeout.Enabled = false;
        options.Idempotency.Enabled = false;
        options.RateLimiter.Enabled = false;
        return new CommandExecutionRuntime(options);
    }
}
