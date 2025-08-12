using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using ManagedCode.Communication.CollectionResultT;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Communication.Tests.Common.TestApp.Controllers;

[Route("test")]
public class TestController : ControllerBase
{
    [HttpGet("test1")]
    public ActionResult<string> Test1()
    {
        throw new ValidationException("ValidationException");
    }

    [HttpGet("test2")]
    public ActionResult<string> Test2()
    {
        throw new InvalidDataException("InvalidDataException");
    }

    [Authorize]
    [HttpGet("test3")]
    public ActionResult<string> Test3()
    {
        return Ok("oke");
    }

    [HttpGet("result-simple")]
    public Result GetResultSuccess()
    {
        return Result.Succeed();
    }

    [HttpGet("result-fail")]
    public Result GetResultFail()
    {
        return Result.Fail("Operation failed", "Something went wrong", HttpStatusCode.BadRequest);
    }

    [HttpGet("result-validation")]
    public Result GetResultValidation()
    {
        return Result.FailValidation(("email", "Email is required"), ("email", "Email format is invalid"), ("age", "Age must be greater than 0"));
    }

    [HttpGet("result-not-found")]
    public Result<string> GetResultNotFound()
    {
        return Result<string>.FailNotFound("User with ID 123 not found");
    }

    [HttpGet("result-unauthorized")]
    public Result GetResultUnauthorized()
    {
        return Result.FailUnauthorized("You need to log in to access this resource");
    }

    [HttpGet("result-forbidden")]
    public Result GetResultForbidden()
    {
        return Result.FailForbidden("You don't have permission to perform this action");
    }

    [HttpGet("result-with-value")]
    public Result<TestModel> GetResultWithValue()
    {
        return Result<TestModel>.Succeed(new TestModel { Id = 42, Name = "Test" });
    }

    [HttpGet("result-custom-enum")]
    public Result GetResultCustomEnum()
    {
        return Result.Fail(TestErrorEnum.InvalidInput, "The input provided is not valid");
    }

    [HttpPost("model-validation")]
    public Result PostWithValidation([FromBody] TestValidationModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.SelectMany(x => x.Value!.Errors.Select(e => (x.Key, e.ErrorMessage)))
                .ToArray();
            return Result.FailValidation(errors);
        }

        return Result.Succeed();
    }

    [HttpGet("exception-to-result")]
    public Result GetExceptionToResult()
    {
        try
        {
            throw new InvalidOperationException("This operation is not allowed");
        }
        catch (Exception ex)
        {
            return Result.Fail(ex);
        }
    }

    [HttpGet("collection-result")]
    public CollectionResult<string> GetCollectionResult()
    {
        var items = new[] { "item1", "item2", "item3" };
        return CollectionResult<string>.Succeed(items, pageNumber: 1, pageSize: 10, totalItems: 3);
    }

    [HttpGet("collection-result-empty")]
    public CollectionResult<string> GetCollectionResultEmpty()
    {
        return CollectionResult<string>.Empty();
    }

    [HttpGet("throw-unhandled")]
    public Result ThrowUnhandledException()
    {
        throw new InvalidOperationException("This is an unhandled exception");
    }

    [HttpGet("throw-exception")]
    public string ThrowException()
    {
        throw new InvalidOperationException("This is a test exception for integration testing");
    }

    [HttpPost("validate")]
    public ActionResult<string> Validate([FromBody] TestValidationModel model)
    {
        return Ok("Validation passed");
    }

    [HttpGet("result-success")]
    public Result<TestModel> GetResultSuccessWithValue()
    {
        return Result<TestModel>.Succeed(new TestModel { Id = 42, Name = "Test Success" });
    }

    [HttpGet("result-failure")]
    public Result<string> GetResultFailure()
    {
        return Result<string>.Fail("Validation failed", "The provided data is invalid", HttpStatusCode.BadRequest);
    }

    [HttpGet("result-notfound")]
    public Result<string> GetResultNotFoundTest()
    {
        return Result<string>.FailNotFound("Resource not found");
    }

    [HttpGet("custom-problem")]
    public ActionResult CustomProblem()
    {
        var problem = Communication.Problem.Create("Custom Error", "This is a custom error for testing", 409, "https://example.com/custom-error");
        return StatusCode(409, problem);
    }

    [HttpGet("collection-success")]
    public CollectionResult<TestModel> GetCollectionSuccess()
    {
        var items = new[]
        {
            new TestModel { Id = 1, Name = "Item 1" },
            new TestModel { Id = 2, Name = "Item 2" },
            new TestModel { Id = 3, Name = "Item 3" }
        };
        return CollectionResult<TestModel>.Succeed(items, pageNumber: 1, pageSize: 10, totalItems: 3);
    }

    [HttpGet("collection-empty")]
    public CollectionResult<TestModel> GetCollectionEmpty()
    {
        return CollectionResult<TestModel>.Empty();
    }

    [HttpGet("enum-error")]
    public Result<string> GetEnumError()
    {
        return Result<string>.Fail(TestErrorEnum.InvalidInput, "Invalid input provided");
    }
}

public class TestModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class TestValidationModel
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = "";

    [Range(1, 120, ErrorMessage = "Age must be between 1 and 120")]
    public int Age { get; set; }
}

public enum TestErrorEnum
{
    InvalidInput,
    DuplicateEntry,
    ResourceLocked,
    QuotaExceeded
}