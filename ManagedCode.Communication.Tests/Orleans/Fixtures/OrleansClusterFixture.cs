using System;
using ManagedCode.Communication.Extensions;
using ManagedCode.Communication.Results.Extensions;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace ManagedCode.Communication.Tests.Orleans.Fixtures;

public class OrleansClusterFixture : IDisposable
{
    public TestCluster Cluster { get; }

    public OrleansClusterFixture()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurator>();
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public void Dispose()
    {
        Cluster?.StopAllSilos();
    }

    private class TestSiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder
                .AddMemoryGrainStorageAsDefault()
                .UseOrleansCommunication();
        }
    }
}
