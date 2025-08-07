using System;
using System.Collections.Generic;
using System.Diagnostics;
using ManagedCode.Communication.AspNetCore.Constants;
using ManagedCode.Communication.AspNetCore.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static ManagedCode.Communication.AspNetCore.Constants.ProblemConstants;

namespace ManagedCode.Communication.AspNetCore.Extensions;

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
    public static IServiceCollection AddCommunication(this IServiceCollection services)
    {
        services.Configure<CommunicationOptions>(options => { });
        return services;
    }

    public static IServiceCollection AddCommunication(this IServiceCollection services, Action<CommunicationOptions> configure)
    {
        services.Configure(configure);
        return services;
    }



    public static IServiceCollection AddCommunicationFilters(this IServiceCollection services)
    {
        services.AddScoped<CommunicationExceptionFilter>();
        services.AddScoped<CommunicationModelValidationFilter>();
        services.AddScoped<CommunicationHubExceptionFilter>();
        services.AddScoped<ResultToActionResultFilter>();

        return services;
    }

    public static MvcOptions AddCommunicationFilters(this MvcOptions options)
    {
        options.Filters.Add<CommunicationExceptionFilter>();
        options.Filters.Add<CommunicationModelValidationFilter>();
        options.Filters.Add<ResultToActionResultFilter>();

        return options;
    }

    public static HubOptions AddCommunicationFilters(this HubOptions options)
    {
        options.AddFilter<CommunicationHubExceptionFilter>();

        return options;
    }
}