using System;
using ManagedCode.Communication.AspNetCore.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Communication.AspNetCore.Extensions;

public static class HubOptionsExtensions
{
    public static void AddCommunicationHubFilter(this HubOptions options)
    {
        options.AddFilter<CommunicationHubExceptionFilter>();
    }

    public static void AddCommunicationFilters(this HubOptions options)
    {
        options.AddCommunicationHubFilter();
    }
}
