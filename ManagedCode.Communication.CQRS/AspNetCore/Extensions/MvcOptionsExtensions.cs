using ManagedCode.Communication.CQRS.AspNetCore.Filters;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Communication.CQRS.AspNetCore.Extensions;

/// <summary>
///     Extension methods for adding CQRS streaming filters to MVC.
/// </summary>
public static class MvcOptionsExtensions
{
    /// <summary>
    ///     Adds CQRS stream mapping filter to MVC options.
    /// </summary>
    public static void AddCommunicationCqrsFilters(this MvcOptions options)
    {
        options.Filters.Add<CqrsResultActionFilter>();
    }
}
