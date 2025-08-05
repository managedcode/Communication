using System;
using ManagedCode.Communication.Extensions.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Communication.Extensions.Extensions;

public static class HubOptionsExtensions
{
    public static void AddCommunicationHubFilter(this HubOptions result, IServiceProvider serviceProvider)
    {
        var hubFilter = serviceProvider.GetRequiredService<CommunicationHubExceptionFilter>();
        result.AddFilter(hubFilter);
    }
}