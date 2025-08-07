using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ManagedCode.Communication.AspNetCore.Extensions;
using ManagedCode.Communication.Extensions;
using ManagedCode.Communication.Tests.Common.TestApp.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using Orleans.TestingHost;
using Xunit;

namespace ManagedCode.Communication.Tests.Common.TestApp;

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
        
        // Manually set content root to avoid solution file discovery
        var contentRoot = FindContentRoot();
        builder.UseContentRoot(contentRoot);
    }
    
    private static string FindContentRoot()
    {
        var currentDir = System.IO.Directory.GetCurrentDirectory();
        var dir = new System.IO.DirectoryInfo(currentDir);
        
        // Navigate up to find solution directory containing .slnx
        while (dir?.Parent != null)
        {
            if (System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "ManagedCode.Communication.slnx")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        
        return currentDir;
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
        Cluster?.StopAllSilosAsync().Wait();
        Cluster?.Dispose();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (Cluster != null)
        {
            await Cluster.StopAllSilosAsync();
            Cluster.Dispose();
        }
    }

    private class TestSiloConfigurations : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.UseOrleansCommunication();
        }
    }

    private class TestClientConfigurations : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
        {
            clientBuilder.UseOrleansCommunication();
        }
    }
}