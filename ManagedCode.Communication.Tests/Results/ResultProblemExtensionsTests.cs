using ManagedCode.Communication;
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
}
