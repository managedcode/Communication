using System;
using ManagedCode.Communication.AspNetCore.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Communication.AspNetCore.Extensions;

/// <summary>
///     Registers the Communication hub filters on <c>HubOptions</c>.
/// </summary>
public static class HubOptionsExtensions
{
    /// <summary>
    ///     Adds the filter that converts hub exceptions into failed results.
    /// </summary>
    public static void AddCommunicationHubFilter(this HubOptions options)
    {
        options.AddFilter<CommunicationHubExceptionFilter>();
    }

    /// <summary>
    ///     Adds every Communication hub filter.
    /// </summary>
    public static void AddCommunicationFilters(this HubOptions options)
    {
        options.AddCommunicationHubFilter();
    }
}
