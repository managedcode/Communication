using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ManagedCode.Communication.Tests.TestApp.Controllers;

public class TestHub : Hub
{
    public Task<int> DoTest()
    {
        return Task.FromResult(new Random().Next());
    }
}