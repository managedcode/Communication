using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Communication.Extensions;
using Xunit;

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
        final.IsSuccess.Should().BeTrue();
        final.Value.Should().Be("Value: 5");
    }

    [Fact]
    public void Then_WithFailedResult_DoesNotExecuteNext()
    {
        // Arrange
        var result = Result<int>.Fail("Initial error");

        // Act
        var final = result.Then(x => Result<string>.Succeed(x.ToString()));

        // Assert
        final.IsFailed.Should().BeTrue();
        final.Problem!.Title.Should().Be("Initial error");
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
        final.IsSuccess.Should().BeTrue();
        final.Value.Should().Be("Async: 10");
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
        final.IsFailed.Should().BeTrue();
        final.Problem.Should().Be(problem);
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
        final.IsSuccess.Should().BeTrue();
        final.Value.Should().Be(15);
    }

    [Fact]
    public void FailIf_WithEnum_FailsWithErrorCode()
    {
        // Arrange
        var result = Result<string>.Succeed("test");

        // Act
        var final = result.FailIf(s => s.Length < 5, TestErrorEnum.InvalidInput);

        // Assert
        final.IsFailed.Should().BeTrue();
        final.Problem!.ErrorCode.Should().Be("InvalidInput");
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
        final.IsFailed.Should().BeTrue();
        var errors = final.Problem!.GetValidationErrors();
        errors!["name"].Should().Contain("Name is required");
        errors["age"].Should().Contain("Must be 18 or older");
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
        final.IsSuccess.Should().BeTrue();
        final.Value.Should().Be(15);
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
        final.IsFailed.Should().BeTrue();
        final.Problem.Should().Be(problem);
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
        merged.IsSuccess.Should().BeTrue();
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
        merged.IsFailed.Should().BeTrue();
        merged.Problem!.Title.Should().Be("Error 2");
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
        merged.IsFailed.Should().BeTrue();
        var errors = merged.Problem!.GetValidationErrors();
        errors!["field1"].Should().Contain("Error 1");
        errors["field2"].Should().Contain("Error 2");
        errors["field3"].Should().Contain("Error 3");
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
        combined.IsSuccess.Should().BeTrue();
        combined.Collection.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void CombineAll_WithMixedResults_CollectsAllErrors()
    {
        // Arrange
        var result1 = Result<string>.Succeed("Success");
        var result2 = Result<string>.FailValidation(("error1", "First error"));
        var result3 = Result<string>.FailValidation(("error2", "Second error"));

        // Act
        var combined = AdvancedRailwayExtensions.CombineAll(result1, result2, result3);

        // Assert
        combined.IsFailed.Should().BeTrue();
        var errors = combined.Problem!.GetValidationErrors();
        errors!["error1"].Should().Contain("First error");
        errors["error2"].Should().Contain("Second error");
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
        successExecuted.Should().BeTrue();
        failureExecuted.Should().BeFalse();
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
        final.IsSuccess.Should().BeTrue();
        final.Value.Should().Be("Medium");
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
        recovered.IsSuccess.Should().BeTrue();
        recovered.Value.Should().Be("Recovered");
    }

    [Fact]
    public void Compensate_WithSuccess_DoesNotExecuteRecovery()
    {
        // Arrange
        var result = Result<string>.Succeed("Original");

        // Act
        var recovered = result.Compensate(problem => Result<string>.Succeed("Recovered"));

        // Assert
        recovered.IsSuccess.Should().BeTrue();
        recovered.Value.Should().Be("Original");
    }

    [Fact]
    public void CompensateWith_WithFailure_ReturnsDefaultValue()
    {
        // Arrange
        var result = Result<int>.Fail("Error");

        // Act
        var recovered = result.CompensateWith(100);

        // Assert
        recovered.IsSuccess.Should().BeTrue();
        recovered.Value.Should().Be(100);
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
        recovered.IsSuccess.Should().BeTrue();
        recovered.Value.Should().Be("Async recovered");
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
        final.IsSuccess.Should().BeTrue();
        checked_.Should().BeTrue();
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
        final.IsFailed.Should().BeTrue();
        final.Problem!.Title.Should().Be("InvalidOperationException");
    }

    [Fact]
    public void Verify_WithTrueCondition_ReturnsSuccess()
    {
        // Arrange
        var result = Result<int>.Succeed(42);

        // Act
        var verified = result.Verify(x => x > 0, "must be positive");

        // Assert
        verified.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Verify_WithFalseCondition_ReturnsFailureWithContext()
    {
        // Arrange
        var result = Result<int>.Succeed(-5);

        // Act
        var verified = result.Verify(x => x > 0, "must be positive");

        // Assert
        verified.IsFailed.Should().BeTrue();
        verified.Problem!.Title.Should().Contain("Verification failed");
        verified.Problem.Detail.Should().Contain("must be positive");
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
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("test");
    }

    [Fact]
    public void ToResult_WithNullValue_ReturnsNotFound()
    {
        // Arrange
        string? value = null;

        // Act
        var result = value.ToResult();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Problem!.StatusCode.Should().Be(404);
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
        result1.IsSuccess.Should().BeTrue();
        result1.Value.Should().Be(42);
        result2.IsFailed.Should().BeTrue();
        result2.Problem.Should().Be(problem);
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
        final.Should().Be(result);
        executed.Should().BeTrue();
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
        final.Should().Be(result);
        executed.Should().BeTrue();
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
        filtered.IsSuccess.Should().BeTrue();
        filtered.Value.Should().Be(42);
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
        filtered.IsFailed.Should().BeTrue();
        filtered.Problem.Should().Be(problem);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("Final: 20");
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
        result.IsFailed.Should().BeTrue();
        var errors = result.Problem!.GetValidationErrors();
        errors.Should().NotBeNull();
        errors!.Should().HaveCount(3);
        errors!["name"].Should().Contain("Name is required");
        errors["email"].Should().Contain("Invalid email format");
        errors["age"].Should().Contain("Must be 18 or older");
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