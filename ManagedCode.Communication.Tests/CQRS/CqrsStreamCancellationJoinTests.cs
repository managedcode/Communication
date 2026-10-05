using System.Threading.Tasks;

namespace ManagedCode.Communication.Tests.CQRS;

public sealed class CqrsStreamCancellationJoinTests
{
    [Test]
    public Task Create_EarlyDisposalJoinsProducerAfterCancellationCallbackFailure()
        => CqrsStreamCancellationJoinScenario.AssertFaultingCallbackJoinAsync();

    [Test]
    public Task Create_OrdinaryEarlyDisposalStillJoinsProducer()
        => CqrsStreamCancellationJoinScenario.AssertOrdinaryCancellationJoinAsync();
}
