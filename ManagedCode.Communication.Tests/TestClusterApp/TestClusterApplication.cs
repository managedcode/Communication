using Orleans.TestingHost;
using Xunit;

namespace ManagedCode.Communication.Tests.TestClusterApp;

[CollectionDefinition(nameof(TestClusterApplication))]
public class TestClusterApplication : ICollectionFixture<TestClusterApplication>
{
    public TestClusterApplication()
    {
        var testClusterBuilder = new TestClusterBuilder();
        Cluster = testClusterBuilder.Build();
        Cluster.Deploy();
    }
    
    public TestCluster Cluster { get; }
}