using Microsoft.AspNetCore.SignalR;

namespace ManagedCode.Communication.Extensions;

public static class HubOptionsExtensions
{
    public static void AddCommunicationHubFilter(this HubOptions result)
    {
        result.AddFilter<CommunicationHubFilter>();
    }
}