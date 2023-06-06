using ManagedCode.Communication.Extensions;
using ManagedCode.Communication.Tests.TestApp.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Communication.Tests.TestApp;

public class HttpHostProgram
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddSignalR();

        
        var app = builder.Build();


        app.MapControllers();
        app.MapHub<TestHub>(nameof(TestHub));
        
        app.UseExceptionHandler("/error");
        
        app.UseCommunication();

        app.Run();
    }
}