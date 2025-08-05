using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using ManagedCode.Communication.Extensions;
using ManagedCode.Communication.Extensions.Filters;

namespace ManagedCode.Communication.Extensions.Extensions;

public static class HubOptionsExtensions
{
    public static void AddCommunicationHubFilter(this HubOptions result, IServiceProvider serviceProvider)
    {
        var hubFilter = serviceProvider.GetRequiredService<HubExceptionFilterBase>();
        result.AddFilter(hubFilter);
    }
}