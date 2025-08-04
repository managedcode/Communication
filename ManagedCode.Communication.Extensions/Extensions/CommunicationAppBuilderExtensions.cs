using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ManagedCode.Communication.Extensions;

namespace ManagedCode.Communication.Extensions.Extensions;

public static class CommunicationAppBuilderExtensions
{
    public static IApplicationBuilder UseCommunication(this IApplicationBuilder app)
    {
        if (app == null)
            throw new ArgumentNullException(nameof(app));

        var serviceProvider = app.ApplicationServices;
        var exceptionFilter = serviceProvider.GetRequiredService<ExceptionFilterBase>();
        var modelValidationFilter = serviceProvider.GetRequiredService<ModelValidationFilterBase>();
        
        var mvcOptions = serviceProvider.GetRequiredService<IOptions<MvcOptions>>();
        mvcOptions.Value.Filters.Add(exceptionFilter);
        mvcOptions.Value.Filters.Add(modelValidationFilter);

        return app;
    }
}