using System;
using System.Net.Http;
using System.Threading.Tasks;
using ManagedCode.Communication.AspNetCore.Extensions;
using ManagedCode.Communication.Extensions;
using ManagedCode.Communication.Orleans.Extensions;
using ManagedCode.Communication.Tests.Common.TestApp.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace ManagedCode.Communication.Tests.Common.TestApp;

public sealed class TestClusterApplication : IAsyncDisposable
{
    public TestClusterApplication()
    {
        var clusterBuilder = new TestClusterBuilder();
        clusterBuilder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        clusterBuilder.AddClientBuilderConfigurator<TestClientConfigurations>();
        Cluster = clusterBuilder.Build();
        Cluster.Deploy();

        var webBuilder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });
        webBuilder.WebHost.UseTestServer();
        webBuilder.Services.AddCommunication(options => { options.ShowErrorDetails = true; });
        webBuilder.Services
            .AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
        webBuilder.Services.AddAuthorization();
        webBuilder.Services.AddControllers(options => { options.AddCommunicationFilters(); });
        webBuilder.Services.AddSignalR(options => { options.AddCommunicationFilters(); });

        Application = webBuilder.Build();
        Application.UseAuthentication();
        Application.UseAuthorization();
        Application.MapControllers();
        Application.MapHub<TestHub>(nameof(TestHub));
        Application.UseCommunication();
        Application.StartAsync().GetAwaiter().GetResult();
        Server = Application.GetTestServer();
    }

    public TestCluster Cluster { get; }

    public WebApplication Application { get; }

    public TestServer Server { get; }

    public HttpClient CreateClient()
    {
        return Server.CreateClient();
    }

    public HubConnection CreateSignalRClient(string hubUrl, Action<HubConnectionBuilder>? configure = null)
    {
        var builder = new HubConnectionBuilder();
        configure?.Invoke(builder);
        return builder.WithUrl(new Uri(Server.BaseAddress, hubUrl), options =>
            {
                options.HttpMessageHandlerFactory = _ => Server.CreateHandler();
            })
            .Build();
    }

    public async ValueTask DisposeAsync()
    {
        await Application.StopAsync();
        await Application.DisposeAsync();
        await Cluster.StopAllSilosAsync();
        Cluster.Dispose();
    }

    private sealed class TestSiloConfigurations : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.UseOrleansCommunication();
        }
    }

    private sealed class TestClientConfigurations : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
        {
            clientBuilder.UseOrleansCommunication();
        }
    }
}
