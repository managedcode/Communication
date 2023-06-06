using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Communication.Extensions;

public static class CommunicationAppBuilderExtensions
{
    public static IApplicationBuilder UseCommunication(this IApplicationBuilder app)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }
        
        return app.UseMiddleware<CommunicationMiddleware>();
    }
}