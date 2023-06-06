using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ManagedCode.Communication.Tests.TestApp.Controllers;

public class TestHub : Hub
{
    public Task<Result<int>> DoTest()
    {
        return Result.Succeed(5).AsTask();
    }
    
    public Task<Result<int>> Throw()
    {
        throw new InvalidDataException("InvalidDataException");
    }
}