using System;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Communication.ZALIPA;
using ManagedCode.Communication.ZALIPA.Result;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManagedCode.Communication.Tests;

public class ResultErrorHandlerTests
{
    [Fact]
    public async Task ResultErrorHandler_Returns_Error_When_ThrowException()
    {
        var resultErrorHandler = new ResultErrorHandler(NullLogger<ResultErrorHandler>.Instance);
        var resultError = await resultErrorHandler.ExecuteAsync(ThrowException);

        resultError.Error.Should().NotBeNull();
    }

    public static Task<Result> ThrowException()
    {
        throw new Exception("Error");
    }
}