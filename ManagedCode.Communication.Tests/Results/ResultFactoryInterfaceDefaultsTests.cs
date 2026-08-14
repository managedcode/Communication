using System;
using System.Collections.Generic;
using System.Net;
using ManagedCode.Communication;
using ManagedCode.Communication.Constants;
using ManagedCode.Communication.Results;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Results;

public class ResultFactoryInterfaceDefaultsTests
{
    private static class ResultFactory<TFactory>
        where TFactory : struct, IResultFactory<TFactory>
    {
        public static TFactory Fail() => TFactory.Fail();
        public static TFactory Fail(string title) => TFactory.Fail(title);
        public static TFactory Fail(string title, string detail) => TFactory.Fail(title, detail);
        public static TFactory Fail(string title, string detail, HttpStatusCode status) => TFactory.Fail(title, detail, status);
        public static TFactory Fail(Exception exception) => TFactory.Fail(exception);
        public static TFactory Fail(Exception exception, HttpStatusCode status) => TFactory.Fail(exception, status);
        public static TFactory Fail<TEnum>(TEnum errorCode) where TEnum : Enum => TFactory.Fail(errorCode);
        public static TFactory Fail<TEnum>(TEnum errorCode, string detail) where TEnum : Enum => TFactory.Fail(errorCode, detail);
        public static TFactory Fail<TEnum>(TEnum errorCode, HttpStatusCode status) where TEnum : Enum => TFactory.Fail(errorCode, status);
        public static TFactory Fail<TEnum>(TEnum errorCode, string detail, HttpStatusCode status) where TEnum : Enum
            => TFactory.Fail(errorCode, detail, status);
        public static TFactory FailBadRequest(string? detail = null) => TFactory.FailBadRequest(detail);
        public static TFactory FailUnauthorized(string? detail = null) => TFactory.FailUnauthorized(detail);
        public static TFactory FailForbidden(string? detail = null) => TFactory.FailForbidden(detail);
        public static TFactory FailNotFound(string? detail = null) => TFactory.FailNotFound(detail);
        public static TFactory Invalid() => TFactory.Invalid();
        public static TFactory Invalid(string message) => TFactory.Invalid(message);
        public static TFactory Invalid(string key, string value) => TFactory.Invalid(key, value);
        public static TFactory Invalid<TEnum>(TEnum code) where TEnum : Enum => TFactory.Invalid(code);
        public static TFactory Invalid<TEnum>(TEnum code, string message) where TEnum : Enum => TFactory.Invalid(code, message);
        public static TFactory Invalid<TEnum>(TEnum code, string key, string value) where TEnum : Enum => TFactory.Invalid(code, key, value);
        public static TFactory Invalid(IEnumerable<KeyValuePair<string, string>> values) => TFactory.Invalid(values);
        public static TFactory Invalid<TEnum>(TEnum code, Dictionary<string, string> values) where TEnum : Enum
            => TFactory.Invalid(code, values);
    }

    private enum TestError
    {
        Bad = 400,
        NotFound = 404,
        Domain = 9999
    }

    [Fact]
    public void IResultFactory_Result_FailDefaults_ShouldCreateFailedResults()
    {
        ResultFactory<Result>.Fail().IsFailed.ShouldBeTrue();
        ResultFactory<Result>.Fail("custom").Problem!.Title.ShouldBe("custom");
        ResultFactory<Result>.Fail("title", "detail").Problem!.Detail.ShouldBe("detail");
        ResultFactory<Result>.Fail("title", "detail", HttpStatusCode.BadRequest).Problem!.StatusCode.ShouldBe(400);
    }

    [Fact]
    public void IResultFactory_Result_FailExceptionDefaults_ShouldMapExceptionsToProblems()
    {
        var failFromException = ResultFactory<Result>.Fail(new InvalidOperationException("boom"));

        failFromException.Problem!.Title.ShouldBe("InvalidOperationException");

        var failFromExceptionWithStatus = ResultFactory<Result>.Fail(new UnauthorizedAccessException("denied"), HttpStatusCode.Forbidden);
        failFromExceptionWithStatus.Problem!.StatusCode.ShouldBe(403);
    }

    [Fact]
    public void IResultFactory_Result_FailEnumDefaults_ShouldConvertErrorCodes()
    {
        ResultFactory<Result>.Fail(TestError.Bad).Problem!.StatusCode.ShouldBe(400);
        ResultFactory<Result>.Fail(TestError.Domain).Problem!.StatusCode.ShouldBe(400);
        ResultFactory<Result>.Fail(TestError.NotFound, "missing").Problem!.Detail.ShouldBe("missing");
        ResultFactory<Result>.Fail(TestError.Domain, "custom", HttpStatusCode.BadGateway).Problem!.StatusCode.ShouldBe(502);
    }

    [Fact]
    public void IResultFactory_Result_FailHttpShortcuts_ShouldMapStatusCodes()
    {
        ResultFactory<Result>.FailBadRequest("bad request").Problem!.StatusCode.ShouldBe(400);
        ResultFactory<Result>.FailUnauthorized("unauth").Problem!.StatusCode.ShouldBe(401);
        ResultFactory<Result>.FailForbidden("forbidden").Problem!.StatusCode.ShouldBe(403);
        ResultFactory<Result>.FailNotFound("missing").Problem!.StatusCode.ShouldBe(404);
    }

    [Fact]
    public void IResultFactory_Result_FailValidation_InvalidFactories_ShouldSetValidationProblem()
    {
        var invalidDefault = ResultFactory<Result>.Invalid();
        var invalidWithMessage = ResultFactory<Result>.Invalid("bad field");
        var invalidWithKeyValue = ResultFactory<Result>.Invalid("key", "value");

        invalidDefault.IsFailed.ShouldBeTrue();
        invalidWithMessage.IsFailed.ShouldBeTrue();
        invalidWithKeyValue.IsFailed.ShouldBeTrue();

        var validationErrors = invalidDefault.Problem?.GetValidationErrors();
        validationErrors.ShouldNotBeNull();
        validationErrors.ShouldContainKey("message");

        ResultFactory<Result>.Invalid(TestError.Bad, "validation").Problem!.GetValidationErrors()?.ShouldNotBeNull();

        var fromEntries = ResultFactory<Result>.Invalid(new Dictionary<string, string> { ["field"] = "error" });
        fromEntries.Problem!.GetValidationErrors()!["field"].ShouldContain("error");

        var fromEntriesEnumerable = ResultFactory<Result>.Invalid(TestError.Bad, new Dictionary<string, string> { ["field"] = "value" });
        fromEntriesEnumerable.Problem!.GetValidationErrors()!.ShouldContainKey("field");

        var invalidEnumEntries = ResultFactory<Result>.Invalid(TestError.NotFound, "field", "value");
        invalidEnumEntries.Problem!.GetValidationErrors()!.ShouldContainKey("field");
        invalidEnumEntries.Problem!.ErrorCode.ShouldBe("NotFound");

        ResultFactory<Result>.Invalid(TestError.Bad, "field", "value").IsFailed.ShouldBeTrue();
    }
}
