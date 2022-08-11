using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManagedCode.Communication.Tests;

public class ResultErrorHandlerTests
{
    [Fact]
    public async Task ResultErrorHandler_Returns_Error_When_ThrowException()
    {
        var resultErrorHandler = new ResultErrorHandler(NullLogger<ResultErrorHandler>.Instance);
        var resultError = await resultErrorHandler.ExecuteAsync<TestResult, TestEnumCode>(ThrowException);

        resultError.Error.Should().NotBeNull();
    }

    public static Task<TestResult> ThrowException()
    {
        throw new Exception("Error");
    }
}

public class TestResult : BaseResult<TestEnumCode>
{
    public TestResult(bool isSuccess) : base(isSuccess)
    {
    }

    public TestResult(Error<TestEnumCode> error) : base(error)
    {
    }

    public TestResult(List<Error<TestEnumCode>> errors) : base(errors)
    {
    }
}

public enum TestEnumCode
{
    Test,
}