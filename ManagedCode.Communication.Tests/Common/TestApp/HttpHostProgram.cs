using ManagedCode.Communication.Extensions.Extensions;
using ManagedCode.Communication.Tests.TestApp;
using ManagedCode.Communication.Tests.TestApp.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Communication.Tests.Common.TestApp;

public class HttpHostProgram
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddCommunication(option =>
        {
            option.ShowErrorDetails = true;
        });
        
        builder.Services.AddCommunicationFilters<TestExceptionFilter, TestModelValidationFilter, TestHubExceptionFilter>();
        
        builder.Services.AddControllers();
        builder.Services.AddSignalR(options => 
        {
        });

        
        var app = builder.Build();


        app.MapControllers();
        app.MapHub<TestHub>(nameof(TestHub));
        
        app.UseCommunication();

        app.Run();
    }
}