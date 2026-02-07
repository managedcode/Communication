using ManagedCode.Communication;
using System.Collections.Generic;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Results;

public class ResultProblemExtensionsTests
{
    [Fact]
    public void Result_ToException_WithNoProblem_ReturnsNull()
    {
        var result = Result.Succeed();

        result.ToException().ShouldBeNull();
    }

    [Fact]
    public void Result_ToException_WithProblem_ReturnsProblemException()
    {
        var result = Result.Fail("oops", "details");

        var exception = result.ToException();

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ProblemException>();
        ((ProblemException)exception!).Problem.ShouldBeSameAs(result.Problem);
    }

    [Fact]
    public void Result_ThrowIfProblem_WithSuccess_DoesNotThrow()
    {
        var result = Result.Succeed();

        Should.NotThrow(result.ThrowIfProblem);
    }

    [Fact]
    public void Result_ThrowIfProblem_WithFailure_ThrowsProblemException()
    {
        var result = Result.Fail("broken", "bad state");

        var exception = Should.Throw<ProblemException>(result.ThrowIfProblem);
        exception.Problem.ShouldBeSameAs(result.Problem);
    }

    [Fact]
    public void ResultT_ToException_WithProblem_ReturnsProblemException()
    {
        var result = Result<string>.Fail("Invalid", "bad");

        var exception = result.ToException();

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ProblemException>();
        ((ProblemException)exception!).Problem.ShouldBeSameAs(result.Problem);
    }

    [Fact]
    public void ResultT_ToException_WithSuccess_ReturnsNull()
    {
        var result = Result<int>.Succeed(5);

        result.ToException().ShouldBeNull();
    }

    [Fact]
    public void ResultT_ThrowIfProblem_WithFailure_ThrowsProblemException()
    {
        var result = Result<int>.Fail("failure", "bad news");

        var exception = Should.Throw<ProblemException>(result.ThrowIfProblem);
        exception.Problem.ShouldBeSameAs(result.Problem);
    }

    [Fact]
    public void Result_ToDisplayMessage_WithErrorCodeResolver_ReturnsResolvedMessage()
    {
        var problem = Problem.Create("Validation Failed", "Raw validation detail", 400);
        problem.ErrorCode = "InvalidInput";
        var result = Result.Fail(problem);

        var message = result.ToDisplayMessage(
            errorCodeResolver: code => code == "InvalidInput" ? "Friendly invalid input message" : null);

        message.ShouldBe("Friendly invalid input message");
    }

    [Fact]
    public void Result_ToDisplayMessage_WithSuccessResult_ReturnsDefaultMessage()
    {
        var result = Result.Succeed();

        var message = result.ToDisplayMessage(defaultMessage: "Everything is fine");

        message.ShouldBe("Everything is fine");
    }

    [Fact]
    public void ResultT_ToDisplayMessage_WithoutDetail_UsesTitle()
    {
        var result = Result<string>.Fail("Not Found", "", System.Net.HttpStatusCode.NotFound);

        var message = result.ToDisplayMessage();

        message.ShouldBe("Not Found");
    }

    [Fact]
    public void Result_ToDisplayMessage_WithDictionaryOverload_ReturnsResolvedMessage()
    {
        var problem = Problem.Create("Registration", "Unavailable", 503);
        problem.ErrorCode = "RegistrationUnavailable";
        var result = Result.Fail(problem);

        var messages = new Dictionary<string, string>
        {
            ["RegistrationUnavailable"] = "Registration is currently unavailable.",
            ["RegistrationBlocked"] = "Registration is temporarily blocked."
        };

        var message = result.ToDisplayMessage(messages, defaultMessage: "Please try again later");

        message.ShouldBe("Registration is currently unavailable.");
    }

    [Fact]
    public void ResultT_ToDisplayMessage_WithTupleOverload_ReturnsResolvedMessage()
    {
        var problem = Problem.Create("Registration", "Unavailable", 503);
        problem.ErrorCode = "RegistrationBlocked";
        var result = Result<string>.Fail(problem);

        var message = result.ToDisplayMessage(
            "Please try again later",
            ("RegistrationUnavailable", "Registration is currently unavailable."),
            ("RegistrationBlocked", "Registration is temporarily blocked."));

        message.ShouldBe("Registration is temporarily blocked.");
    }
}
