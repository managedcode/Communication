using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ManagedCode.Communication.Extensions.Extensions;


public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddCommunication(this IHostApplicationBuilder builder)
    {
        builder.Services.AddCommunication(options => options.ShowErrorDetails = builder.Environment.IsDevelopment());
        return builder;
    }
    
    public static IHostApplicationBuilder AddCommunication(this IHostApplicationBuilder builder, Action<CommunicationOptions> config)
    {
        builder.Services.AddCommunication(config);
        return builder;
    }
}

public static class ServiceCollectionExtensions
{
    
    public static IServiceCollection AddCommunication(this IServiceCollection services, Action<CommunicationOptions> options)
    {
        services.AddOptions<CommunicationOptions>()
            .Configure(options);
        
        return services;
    }
    
   

    public static IServiceCollection AddDefaultProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                var statusCode = context.ProblemDetails.Status.GetValueOrDefault(StatusCodes.Status500InternalServerError);

                context.ProblemDetails.Type ??= $"https://httpstatuses.io/{statusCode}";
                context.ProblemDetails.Title ??= ReasonPhrases.GetReasonPhrase(statusCode);
                context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
                context.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
            };
        });

        return services;
    }

    public static IServiceCollection AddCommunicationExceptionHandler(this IServiceCollection services)
    {
        // Ensures that the ProblemDetails service is registered.
        services.AddProblemDetails();

        services.AddExceptionHandler<CommunicationExceptionHandler>();
        return services;
    }
}