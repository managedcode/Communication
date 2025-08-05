using ManagedCode.Communication.Extensions.Extensions;
using ManagedCode.Communication.Tests.Common.TestApp.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Communication.Tests.Common.TestApp;

public class HttpHostProgram
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddCommunication(option => { option.ShowErrorDetails = true; });

        builder.Services
            .AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options => { });

        builder.Services.AddAuthorization();

        builder.Services.AddCommunicationFilters();

        builder.Services.AddControllers(options => { options.AddCommunicationFilters(); });

        builder.Services.AddSignalR(options => { options.AddCommunicationFilters(); });


        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHub<TestHub>(nameof(TestHub));

        app.UseCommunication();

        app.Run();
    }
}