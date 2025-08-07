using System;
using Microsoft.AspNetCore.Builder;

namespace ManagedCode.Communication.AspNetCore.Extensions;

public static class CommunicationAppBuilderExtensions
{
    /// <summary>
    /// Configures Communication middleware pipeline. 
    /// NOTE: This method currently serves as a placeholder for future middleware registration.
    /// Communication functionality is primarily handled through filters registered via AddCommunicationFilters().
    /// </summary>
    public static IApplicationBuilder UseCommunication(this IApplicationBuilder app)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        // Currently no middleware registration needed - 
        // Communication functionality is handled via filters
        // Future middleware can be added here as needed
        
        return app;
    }
}