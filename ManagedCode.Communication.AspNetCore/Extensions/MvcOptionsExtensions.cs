using ManagedCode.Communication.AspNetCore.Filters;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Communication.AspNetCore.Extensions;

/// <summary>
/// Extension methods for configuring MVC options with Communication filters
/// </summary>
public static class MvcOptionsExtensions
{
    /// <summary>
    /// Adds Communication filters to MVC options in the correct order
    /// </summary>
    public static void AddCommunicationFilters(this MvcOptions options)
    {
        // Add filters in the correct order for proper functionality
        options.Filters.Add<CommunicationModelValidationFilter>();
        options.Filters.Add<CommunicationExceptionFilter>();
        options.Filters.Add<ResultToActionResultFilter>();
    }
}