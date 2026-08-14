using Microsoft.AspNetCore.Mvc;
using CqrsMvcOptionsExtensions = ManagedCode.Communication.CQRS.AspNetCore.Extensions.MvcOptionsExtensions;

namespace ManagedCode.Communication.AspNetCore.Extensions;

/// <summary>
///     Facade over the CQRS MVC filter registration for applications that depend only on the monolithic
///     <c>ManagedCode.Communication.AspNetCore</c> package.
/// </summary>
public static class CommunicationCqrsMvcOptionsExtensions
{
    /// <inheritdoc cref="CqrsMvcOptionsExtensions.AddCommunicationCqrsFilters" />
    public static MvcOptions AddCommunicationCqrsFilters(this MvcOptions options)
    {
        return CqrsMvcOptionsExtensions.AddCommunicationCqrsFilters(options);
    }
}
