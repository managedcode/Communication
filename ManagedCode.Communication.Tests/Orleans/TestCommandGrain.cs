using System.Threading.Tasks;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Commands;
using Orleans;

namespace ManagedCode.Communication.Tests.Orleans;

public class TestCommandGrain : Grain, ITestCommandGrain
{
    public Task<Command> EchoCommandAsync(Command command)
    {
        // Simply return the command back to test serialization/deserialization
        return Task.FromResult(command);
    }

    public Task<Command<T>> EchoCommandAsync<T>(Command<T> command)
    {
        // Simply return the command back to test serialization/deserialization
        return Task.FromResult(command);
    }

    public Task<Result> EchoResultAsync(Result result)
    {
        return Task.FromResult(result);
    }

    public Task<Result<T>> EchoResultAsync<T>(Result<T> result)
    {
        return Task.FromResult(result);
    }

    public Task<CollectionResult<T>> EchoCollectionResultAsync<T>(CollectionResult<T> result)
    {
        return Task.FromResult(result);
    }

    public Task<Problem> EchoProblemAsync(Problem problem)
    {
        return Task.FromResult(problem);
    }
}