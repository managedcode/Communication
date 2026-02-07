using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Shouldly;
using ManagedCode.Communication.Constants;
using ManagedCode.Communication.Extensions;
using ManagedCode.Communication.Results.Extensions;
using Xunit;
using ManagedCode.Communication.Tests.TestHelpers;

namespace ManagedCode.Communication.Tests.Extensions;

public class AdvancedRailwayExtensionsTests
{
    #region Then/ThenAsync Tests

    [Fact]
    public void Then_WithSuccessfulResult_ExecutesNext()
    {
        // Arrange
        var result = Result<int>.Succeed(5);

        // Act
        var final = result
            .Then(x => Result<string>.Succeed(x.ToString()))
            .Then(s => Result<string>.Succeed($"Value: {s}"));

        // Assert
        final.IsSuccess.ShouldBeTrue();
        final.Value.ShouldBe("Value: 5");
    }

    [Fact]
    public void Then_WithFailedResult_DoesNotExecuteNext()
    {
        // Arrange
        var result = Result<int>.Fail("Initial error");

        // Act
        var final = result.Then(x => Result<string>.Succeed(x.ToString()));

        // Assert
        final.IsFailed.ShouldBeTrue();
        final.Problem!.Title.ShouldBe("Initial error");
    }

    [Fact]
    public async Task ThenAsync_WithSuccessfulResult_ExecutesNext()
    {
        // Arrange
        var result = Result<int>.Succeed(10);

        // Act
        var final = await result.ThenAsync(async x =>
        {
            await Task.Delay(1);
            return Result<string>.Succeed($"Async: {x}");
        });

        // Assert
        final.IsSuccess.ShouldBeTrue();
        final.Value.ShouldBe("Async: 10");
    }

    #endregion

    #region FailIf/OkIf Tests

    [Fact]
    public void FailIf_WithTrueCondition_FailsResult()
    {
        // Arrange
        var result = Result<int>.Succeed(5);
        var problem = Problem.Create("Too small", "Value must be greater than 10", 400);

        // Act
        var final = result.FailIf(x => x < 10, problem);

        // Assert
        final.IsFailed.ShouldBeTrue();
        final.Problem.ShouldBe(problem);
    }

    [Fact]
    public void FailIf_WithFalseCondition_KeepsSuccess()
    {
        // Arrange
        var result = Result<int>.Succeed(15);
        var problem = Problem.Create("Too small", "Value must be greater than 10", 400);

        // Act
        var final = result.FailIf(x => x < 10, problem);

        // Assert
        final.IsSuccess.ShouldBeTrue();
        final.Value.ShouldBe(15);
    }

    [Fact]
    public void FailIf_WithEnum_FailsWithErrorCode()
    {
        // Arrange
        var result = Result<string>.Succeed("test");

        // Act
        var final = result.FailIf(s => s.Length < 5, TestErrorEnum.InvalidInput);

        // Assert
        final.IsFailed.ShouldBeTrue();
        final.Problem!.ErrorCode.ShouldBe("InvalidInput");
    }

    [Fact]
    public void FailIf_WithValidationErrors_FailsWithMultipleErrors()
    {
        // Arrange
        var result = Result<User>.Succeed(new User { Name = "", Age = 10 });

        // Act
        var final = result.FailIf(
            u => string.IsNullOrEmpty(u.Name) || u.Age < 18,
            ("name", "Name is required"),
            ("age", "Must be 18 or older")
        );

        // Assert
        final.IsFailed.ShouldBeTrue();
        var errors = final.Problem!.GetValidationErrors();
        errors!["name"].ShouldContain("Name is required");
        errors["age"].ShouldContain("Must be 18 or older");
    }

    [Fact]
    public void OkIf_WithTrueCondition_KeepsSuccess()
    {
        // Arrange
        var result = Result<int>.Succeed(15);
        var problem = Problem.Create("Invalid", "Value validation failed", 400);

        // Act
        var final = result.OkIf(x => x > 10, problem);

        // Assert
        final.IsSuccess.ShouldBeTrue();
        final.Value.ShouldBe(15);
    }

    [Fact]
    public void OkIf_WithFalseCondition_FailsResult()
    {
        // Arrange
        var result = Result<int>.Succeed(5);
        var problem = Problem.Create("Invalid", "Value must be greater than 10", 400);

        // Act
        var final = result.OkIf(x => x > 10, problem);

        // Assert
        final.IsFailed.ShouldBeTrue();
        final.Problem.ShouldBe(problem);
    }

    #endregion

    #region Merge/Combine Tests

    [Fact]
    public void Merge_WithAllSuccessful_ReturnsSuccess()
    {
        // Arrange
        var result1 = Result.Succeed();
        var result2 = Result.Succeed();
        var result3 = Result.Succeed();

        // Act
        var merged = AdvancedRailwayExtensions.Merge(result1, result2, result3);

        // Assert
        merged.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Merge_WithOneFailure_ReturnsFirstFailure()
    {
        // Arrange
        var result1 = Result.Succeed();
        var result2 = Result.Fail("Error 2");
        var result3 = Result.Succeed();

        // Act
        var merged = AdvancedRailwayExtensions.Merge(result1, result2, result3);

        // Assert
        merged.IsFailed.ShouldBeTrue();
        merged.Problem!.Title.ShouldBe("Error 2");
    }

    [Fact]
    public void MergeAll_WithMultipleFailures_CollectsAllErrors()
    {
        // Arrange
        var result1 = Result.FailValidation(("field1", "Error 1"));
        var result2 = Result.FailValidation(("field2", "Error 2"));
        var result3 = Result.FailValidation(("field3", "Error 3"));

        // Act
        var merged = AdvancedRailwayExtensions.MergeAll(result1, result2, result3);

        // Assert
        merged.IsFailed.ShouldBeTrue();
        var errors = merged.Problem!.GetValidationErrors();
        errors!["field1"].ShouldContain("Error 1");
        errors["field2"].ShouldContain("Error 2");
        errors["field3"].ShouldContain("Error 3");
    }

    [Fact]
    public void MergeAll_WithMixedFailures_ReturnsAggregateProblemWithOriginalErrors()
    {
        // Arrange
        var result1 = Result.Fail("Unauthorized", "Authentication required", HttpStatusCode.Unauthorized);
        var result2 = Result.Fail("Forbidden", "Access denied", HttpStatusCode.Forbidden);
        var result3 = Result.Fail("Server Error", "Unexpected failure", HttpStatusCode.InternalServerError);

        // Act
        var merged = AdvancedRailwayExtensions.MergeAll(result1, result2, result3);

        // Assert
        merged.IsFailed.ShouldBeTrue();
        merged.Problem.ShouldNotBeNull();
        merged.Problem!.Title.ShouldBe("Multiple errors occurred");
        merged.Problem.Detail.ShouldBe("The operation failed with multiple errors.");
        merged.Problem.StatusCode.ShouldBe(500);
        merged.Problem.TryGetExtension(ProblemConstants.ExtensionKeys.Errors, out Problem[]? aggregatedErrors).ShouldBeTrue();
        aggregatedErrors.ShouldNotBeNull();
        aggregatedErrors.Length.ShouldBe(3);
        aggregatedErrors.Select(problem => problem.StatusCode).ShouldBeEquivalentTo(new[] { 401, 403, 500 });
    }

    [Fact]
    public void MergeAll_WithValidationAndHttpFailures_ReturnsAggregateProblem()
    {
        // Arrange
        var result1 = Result.FailValidation(("email", "Email is invalid"));
        var result2 = Result.Fail("Unauthorized", "Authentication required", HttpStatusCode.Unauthorized);

        // Act
        var merged = AdvancedRailwayExtensions.MergeAll(result1, result2);

        // Assert
        merged.IsFailed.ShouldBeTrue();
        merged.Problem.ShouldNotBeNull();
        merged.Problem!.StatusCode.ShouldBe(500);
        merged.Problem.TryGetExtension(ProblemConstants.ExtensionKeys.Errors, out Problem[]? aggregatedErrors).ShouldBeTrue();
        aggregatedErrors.ShouldNotBeNull();
        aggregatedErrors.Length.ShouldBe(2);
        aggregatedErrors.Select(problem => problem.StatusCode).ShouldContain(400);
        aggregatedErrors.Select(problem => problem.StatusCode).ShouldContain(401);
    }

    [Fact]
    public void Combine_WithAllSuccessful_ReturnsAllValues()
    {
        // Arrange
        var result1 = Result<int>.Succeed(1);
        var result2 = Result<int>.Succeed(2);
        var result3 = Result<int>.Succeed(3);

        // Act
        var combined = AdvancedRailwayExtensions.Combine(result1, result2, result3);

        // Assert
        combined.IsSuccess.ShouldBeTrue();
        combined.Collection.ShouldBeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void CombineAll_WithValidationFailures_CollectsAllErrors()
    {
        // Arrange
        var result1 = Result<string>.Succeed("Success");
        var result2 = Result<string>.FailValidation(("error1", "First error"));
        var result3 = Result<string>.FailValidation(("error2", "Second error"));

        // Act
        var combined = AdvancedRailwayExtensions.CombineAll(result1, result2, result3);

        // Assert
        combined.IsFailed.ShouldBeTrue();
        var errors = combined.Problem!.GetValidationErrors();
        errors!["error1"].ShouldContain("First error");
        errors["error2"].ShouldContain("Second error");
    }

    [Fact]
    public void CombineAll_WithMixedFailures_ReturnsAggregateProblem()
    {
        // Arrange
        var result1 = Result<string>.Succeed("Success");
        var result2 = Result<string>.Fail("Unauthorized", "Authentication required", HttpStatusCode.Unauthorized);
        var result3 = Result<string>.FailValidation(("email", "Email is invalid"));

        // Act
        var combined = AdvancedRailwayExtensions.CombineAll(result1, result2, result3);

        // Assert
        combined.IsFailed.ShouldBeTrue();
        combined.Problem.ShouldNotBeNull();
        combined.Problem!.Title.ShouldBe("Multiple errors occurred");
        combined.Problem.StatusCode.ShouldBe(500);
        combined.Problem.TryGetExtension(ProblemConstants.ExtensionKeys.Errors, out Problem[]? aggregatedErrors).ShouldBeTrue();
        aggregatedErrors.ShouldNotBeNull();
        aggregatedErrors.Length.ShouldBe(2);
        aggregatedErrors.Select(problem => problem.StatusCode).ShouldContain(401);
        aggregatedErrors.Select(problem => problem.StatusCode).ShouldContain(400);
    }

    #endregion

    #region Switch/Case Tests

    [Fact]
    public void Switch_WithSuccess_ExecutesSuccessAction()
    {
        // Arrange
        var result = Result<int>.Succeed(42);
        var successExecuted = false;
        var failureExecuted = false;

        // Act
        result.Switch(
            onSuccess: v => successExecuted = true,
            onFailure: p => failureExecuted = true
        );

        // Assert
        successExecuted.ShouldBeTrue();
        failureExecuted.ShouldBeFalse();
    }

    [Fact]
    public void SwitchFirst_WithMatchingCondition_ExecutesCorrectCase()
    {
        // Arrange
        var result = Result<int>.Succeed(15);

        // Act
        var final = result.SwitchFirst(
            (x => x < 10, x => Result<string>.Succeed("Small")),
            (x => x >= 10 && x < 20, x => Result<string>.Succeed("Medium")),
            (x => x >= 20, x => Result<string>.Succeed("Large"))
        );

        // Assert
        final.IsSuccess.ShouldBeTrue();
        final.Value.ShouldBe("Medium");
    }

    #endregion

    #region Compensate/Recover Tests

    [Fact]
    public void Compensate_WithFailure_ExecutesRecovery()
    {
        // Arrange
        var result = Result<string>.Fail("Original error");

        // Act
        var recovered = result.Compensate(problem => Result<string>.Succeed("Recovered"));

        // Assert
        recovered.IsSuccess.ShouldBeTrue();
        recovered.Value.ShouldBe("Recovered");
    }

    [Fact]
    public void Compensate_WithSuccess_DoesNotExecuteRecovery()
    {
        // Arrange
        var result = Result<string>.Succeed("Original");

        // Act
        var recovered = result.Compensate(problem => Result<string>.Succeed("Recovered"));

        // Assert
        recovered.IsSuccess.ShouldBeTrue();
        recovered.Value.ShouldBe("Original");
    }

    [Fact]
    public void CompensateWith_WithFailure_ReturnsDefaultValue()
    {
        // Arrange
        var result = Result<int>.Fail("Error");

        // Act
        var recovered = result.CompensateWith(100);

        // Assert
        recovered.IsSuccess.ShouldBeTrue();
        recovered.Value.ShouldBe(100);
    }

    [Fact]
    public async Task CompensateAsync_WithFailure_ExecutesAsyncRecovery()
    {
        // Arrange
        var result = Result<string>.Fail("Error");

        // Act
        var recovered = await result.CompensateAsync(async problem =>
        {
            await Task.Delay(1);
            return Result<string>.Succeed("Async recovered");
        });

        // Assert
        recovered.IsSuccess.ShouldBeTrue();
        recovered.Value.ShouldBe("Async recovered");
    }

    #endregion

    #region Check/Verify Tests

    [Fact]
    public void Check_WithSuccessAndNoException_ReturnsSuccess()
    {
        // Arrange
        var result = Result<int>.Succeed(42);
        var checked_ = false;

        // Act
        var final = result.Check(x => checked_ = true);

        // Assert
        final.IsSuccess.ShouldBeTrue();
        checked_.ShouldBeTrue();
    }

    [Fact]
    public void Check_WithException_ReturnsFailure()
    {
        // Arrange
        var result = Result<int>.Succeed(0);

        // Act
        var final = result.Check(x =>
        {
            if (x == 0) throw new InvalidOperationException("Cannot be zero");
        });

        // Assert
        final.IsFailed.ShouldBeTrue();
        final.Problem!.Title.ShouldBe("InvalidOperationException");
    }

    [Fact]
    public void Verify_WithTrueCondition_ReturnsSuccess()
    {
        // Arrange
        var result = Result<int>.Succeed(42);

        // Act
        var verified = result.Verify(x => x > 0, "must be positive");

        // Assert
        verified.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Verify_WithFalseCondition_ReturnsFailureWithContext()
    {
        // Arrange
        var result = Result<int>.Succeed(-5);

        // Act
        var verified = result.Verify(x => x > 0, "must be positive");

        // Assert
        verified.IsFailed.ShouldBeTrue();
        verified.Problem!.Title!.ShouldContain("Verification failed");
        verified.Problem.Detail!.ShouldContain("must be positive");
    }

    #endregion

    #region ToResult Tests

    [Fact]
    public void ToResult_WithNonNullValue_ReturnsSuccess()
    {
        // Arrange
        string? value = "test";

        // Act
        var result = value.ToResult();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("test");
    }

    [Fact]
    public void ToResult_WithNullValue_ReturnsNotFound()
    {
        // Arrange
        string? value = null;

        // Act
        var result = value.ToResult();

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem!.StatusCode.ShouldBe(404);
    }

    [Fact]
    public void ToResult_WithNullableStruct_HandlesCorrectly()
    {
        // Arrange
        int? hasValue = 42;
        int? noValue = null;
        var problem = Problem.Create("Missing", "Value is required", 400);

        // Act
        var result1 = hasValue.ToResult(problem);
        var result2 = noValue.ToResult(problem);

        // Assert
        result1.IsSuccess.ShouldBeTrue();
        result1.Value.ShouldBe(42);
        result2.IsFailed.ShouldBeTrue();
        result2.Problem.ShouldBe(problem);
    }

    #endregion

    #region Do/Execute Tests

    [Fact]
    public void Do_WithSuccess_ExecutesAction()
    {
        // Arrange
        var result = Result<int>.Succeed(42);
        var executed = false;

        // Act
        var final = result.Do(x => executed = true);

        // Assert
        final.ShouldBe(result);
        executed.ShouldBeTrue();
    }

    [Fact]
    public async Task DoAsync_WithSuccess_ExecutesAsyncAction()
    {
        // Arrange
        var result = Result<int>.Succeed(42);
        var executed = false;

        // Act
        var final = await result.DoAsync(async x =>
        {
            await Task.Delay(1);
            executed = true;
        });

        // Assert
        final.ShouldBe(result);
        executed.ShouldBeTrue();
    }

    #endregion

    #region Filter/Where Tests

    [Fact]
    public void Where_WithMatchingPredicate_ReturnsSuccess()
    {
        // Arrange
        var result = Result<int>.Succeed(42);

        // Act
        var filtered = result.Where(x => x > 10, "Value must be greater than 10");

        // Assert
        filtered.IsSuccess.ShouldBeTrue();
        filtered.Value.ShouldBe(42);
    }

    [Fact]
    public void Where_WithNonMatchingPredicate_ReturnsFailure()
    {
        // Arrange
        var result = Result<int>.Succeed(5);
        var problem = Problem.Create("Filter", "Not matching", 400);

        // Act
        var filtered = result.Where(x => x > 10, problem);

        // Assert
        filtered.IsFailed.ShouldBeTrue();
        filtered.Problem.ShouldBe(problem);
    }

    #endregion

    #region Complex Railway Chains

    [Fact]
    public async Task ComplexRailwayChain_DemonstratesFullCapabilities()
    {
        // Arrange
        var initialValue = 10;

        // Act
        var intermediateResult = Result<int>.Succeed(initialValue)
            .Then(x => Result<int>.Succeed(x * 2))             // Double it
            .FailIf(x => x > 100, Problem.Create("Too large", "Value exceeded limit", 400))
            .Where(x => x % 2 == 0, "Must be even")            // Verify it's even
            .Do(x => Console.WriteLine($"Value: {x}"));        // Side effect
            
        var asyncResult = await intermediateResult.ThenAsync(async x =>  // Async operation
        {
            await Task.Delay(1);
            return Result<string>.Succeed($"Final: {x}");
        });
        
        var result = await asyncResult.CompensateAsync(async problem =>  // Recovery if failed
        {
            await Task.Delay(1);
            return Result<string>.Succeed("Recovered");
        });

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("Final: 20");
    }

    [Fact]
    public void ParallelValidation_CollectsAllErrors()
    {
        // Arrange
        var user = new User { Name = "", Email = "invalid", Age = 10 };

        var nameValidation = string.IsNullOrEmpty(user.Name)
            ? Result.FailValidation(("name", "Name is required"))
            : Result.Succeed();

        var emailValidation = !user.Email.Contains("@")
            ? Result.FailValidation(("email", "Invalid email format"))
            : Result.Succeed();

        var ageValidation = user.Age < 18
            ? Result.FailValidation(("age", "Must be 18 or older"))
            : Result.Succeed();

        // Act
        var result = AdvancedRailwayExtensions.MergeAll(nameValidation, emailValidation, ageValidation);

        // Assert
        result.IsFailed.ShouldBeTrue();
        var errors = result.AssertValidationErrors();
        errors.ShouldNotBeNull();
        errors!.ShouldHaveCount(3);
        errors!["name"].ShouldContain("Name is required");
        errors["email"].ShouldContain("Invalid email format");
        errors["age"].ShouldContain("Must be 18 or older");
    }

    #endregion

    #region Test Helpers

    private enum TestErrorEnum
    {
        InvalidInput,
        ValidationFailed
    }

    private class User
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public int Age { get; set; }
    }

    #endregion
}
