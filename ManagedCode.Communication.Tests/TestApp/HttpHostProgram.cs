using ManagedCode.Communication.Extensions;
using ManagedCode.Communication.Extensions.Extensions;
using ManagedCode.Communication.Tests.TestApp.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Communication.Tests.TestApp;

public class HttpHostProgram
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddCommunication(option =>
        {
            option.ShowErrorDetails = true;
        });
        
        builder.Services.AddControllers();
        builder.Services.AddSignalR(options => options.AddCommunicationHubFilter());

        
        var app = builder.Build();


        app.MapControllers();
        app.MapHub<TestHub>(nameof(TestHub));
        
        app.UseCommunication();

        app.Run();
    }
}