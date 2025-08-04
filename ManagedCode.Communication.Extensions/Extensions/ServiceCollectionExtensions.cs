using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static ManagedCode.Communication.Extensions.Constants.ProblemConstants;

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
    
    public static IServiceCollection AddCommunication(this IServiceCollection services, Action<CommunicationOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);

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
                context.ProblemDetails.Extensions.TryAdd(ExtensionKeys.TraceId, Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
            };
        });

        return services;
    }

    public static IServiceCollection AddCommunicationFilters<TExceptionFilter, TModelValidationFilter, THubExceptionFilter>(
        this IServiceCollection services)
        where TExceptionFilter : ExceptionFilterBase
        where TModelValidationFilter : ModelValidationFilterBase
        where THubExceptionFilter : HubExceptionFilterBase
    {
        services.AddScoped<TExceptionFilter>();
        services.AddScoped<TModelValidationFilter>();
        services.AddScoped<THubExceptionFilter>();

        services.AddControllers(options => 
        { 
            options.Filters.Add<TExceptionFilter>();
            options.Filters.Add<TModelValidationFilter>();
        });

        services.Configure<HubOptions>(options =>
        {
            options.AddFilter<THubExceptionFilter>();
        });

        return services;
    }
}