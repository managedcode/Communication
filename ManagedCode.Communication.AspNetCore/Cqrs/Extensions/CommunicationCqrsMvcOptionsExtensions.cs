using System;
using System.Linq;
using ManagedCode.Communication.AspNetCore.Filters;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Communication.AspNetCore.Extensions;

/// <summary>
///     Registers CQRS streaming filters on <see cref="MvcOptions" />.
/// </summary>
public static class CommunicationCqrsMvcOptionsExtensions
{
    /// <summary>
    ///     Adds the CQRS stream result filter to MVC. Calling this more than once is a no-op, so the filter never
    ///     ends up registered twice.
    /// </summary>
    public static MvcOptions AddCommunicationCqrsFilters(this MvcOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Filters.OfType<TypeFilterAttribute>()
            .Any(static filter => filter.ImplementationType == typeof(CqrsResultActionFilter)))
        {
            return options;
        }

        options.Filters.Add<CqrsResultActionFilter>();
        return options;
    }
}
