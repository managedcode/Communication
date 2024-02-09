using System;
using System.Threading.Tasks;
using ManagedCode.Communication.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using Orleans.TestingHost;
using Xunit;

namespace ManagedCode.Communication.Tests.TestApp;

[CollectionDefinition(nameof(TestClusterApplication))]
public class TestClusterApplication : WebApplicationFactory<HttpHostProgram>, ICollectionFixture<TestClusterApplication>
{
    public TestClusterApplication()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        builder.AddClientBuilderConfigurator<TestClientConfigurations>();
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public TestCluster Cluster { get; }
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        return base.CreateHost(builder);
    }

    public HubConnection CreateSignalRClient(string hubUrl, Action<HubConnectionBuilder>? configure = null)
    {
        var builder = new HubConnectionBuilder();
        configure?.Invoke(builder);
        return builder.WithUrl(new Uri(Server.BaseAddress, hubUrl), o => o.HttpMessageHandlerFactory = _ => Server.CreateHandler())
            .Build();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
    }
    
    class TestSiloConfigurations : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.UseOrleansCommunication();
        }
    }

    class TestClientConfigurations : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
        {
            clientBuilder.UseOrleansCommunication();
        }
    }
}