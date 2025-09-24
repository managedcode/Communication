using System.Threading.Tasks;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Commands;
using Orleans;

namespace ManagedCode.Communication.Tests.Orleans.Grains;

/// <summary>
/// Test grain interface for verifying serialization of all Communication types
/// </summary>
public interface ITestSerializationGrain : IGrainWithGuidKey
{
    /// <summary>
    /// Echo back the command to verify all fields are preserved
    /// </summary>
    Task<Command> EchoCommandAsync(Command command);
    
    /// <summary>
    /// Echo back the typed command to verify all fields are preserved
    /// </summary>
    Task<Command<T>> EchoCommandAsync<T>(Command<T> command);
    
    /// <summary>
    /// Test Result serialization
    /// </summary>
    Task<Result> EchoResultAsync(Result result);
    
    /// <summary>
    /// Test Result with value serialization
    /// </summary>
    Task<Result<T>> EchoResultAsync<T>(Result<T> result);

    /// <summary>
    /// Test pagination command serialization
    /// </summary>
    Task<PaginationCommand> EchoPaginationCommandAsync(PaginationCommand command);

    /// <summary>
    /// Test pagination request serialization
    /// </summary>
    Task<PaginationRequest> EchoPaginationRequestAsync(PaginationRequest request);
    
    /// <summary>
    /// Test CollectionResult serialization
    /// </summary>
    Task<CollectionResult<T>> EchoCollectionResultAsync<T>(CollectionResult<T> result);
    
    /// <summary>
    /// Test Problem serialization
    /// </summary>
    Task<Problem> EchoProblemAsync(Problem problem);
    
    /// <summary>
    /// Test CommandMetadata serialization
    /// </summary>
    Task<CommandMetadata> EchoMetadataAsync(CommandMetadata metadata);
}
