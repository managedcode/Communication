using CqrsMvcOptionsExtensions = ManagedCode.Communication.CQRS.AspNetCore.Extensions.MvcOptionsExtensions;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Communication.AspNetCore.Extensions;

/// <summary>
///     Backward/monolithic package facade for CQRS MVC filter registration.
/// </summary>
public static class CommunicationCqrsMvcOptionsExtensions
{
    /// <summary>
    ///     Adds CQRS result mapping filters to MVC.
    /// </summary>
    public static void AddCommunicationCqrsFilters(this MvcOptions options)
    {
        CqrsMvcOptionsExtensions.AddCommunicationCqrsFilters(options);
    }
}
