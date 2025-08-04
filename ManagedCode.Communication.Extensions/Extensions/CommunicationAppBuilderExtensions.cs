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

        // Note: Filters are now registered automatically via AddCommunicationFilters() in ConfigureServices
        // This method is kept for backward compatibility but no longer performs filter registration
        // Use AddCommunicationFilters<TExceptionFilter, TModelValidationFilter, THubExceptionFilter>() instead

        return app;
    }
}