using System;
using Microsoft.AspNetCore.Builder;

namespace ManagedCode.Communication.Extensions.Extensions;

public static class CommunicationAppBuilderExtensions
{
    public static IApplicationBuilder UseCommunication(this IApplicationBuilder app)
    {
        if (app == null)
            throw new ArgumentNullException(nameof(app));

        return app.UseMiddleware<CommunicationMiddleware>();
    }
}