using System;
using System.Net;
using ManagedCode.Communication.Helpers;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Helpers;

/// <summary>
///     Default exception classification and the application overrides on top of it.
/// </summary>
/// <remarks>
///     Not run in parallel with anything else: the override map is process-wide by design, since it is meant to be
///     configured once at startup.
/// </remarks>
[Collection(nameof(ExceptionStatusCodeMapTests))]
[CollectionDefinition(nameof(ExceptionStatusCodeMapTests), DisableParallelization = true)]
public sealed class ExceptionStatusCodeMapTests : IDisposable
{
    public void Dispose()
    {
        ExceptionStatusCodeMap.Reset();
    }

    private sealed class OrderNotFoundException() : Exception("missing");

    private sealed class DomainRuleException() : InvalidOperationException("rule broken");

    // ---------- defaults ----------

    [Theory]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(NotSupportedException))]
    [InlineData(typeof(InvalidCastException))]
    [InlineData(typeof(NullReferenceException))]
    [InlineData(typeof(IndexOutOfRangeException))]
    public void ServerSideFaultsAre500(Type exceptionType)
    {
        // These say the server reached a state its own code did not allow for. Reporting them as 400 blames the
        // caller and keeps the defect out of every 5xx dashboard.
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;

        HttpStatusCodeHelper.GetStatusCodeForException(exception)
            .ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Theory]
    [InlineData(typeof(ArgumentException))]
    [InlineData(typeof(ArgumentNullException))]
    [InlineData(typeof(ArgumentOutOfRangeException))]
    [InlineData(typeof(FormatException))]
    public void MalformedInputIs400(Type exceptionType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;

        HttpStatusCodeHelper.GetStatusCodeForException(exception)
            .ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void NullIsRejected()
    {
        Should.Throw<ArgumentNullException>(() => HttpStatusCodeHelper.GetStatusCodeForException(null!));
    }

    // ---------- overrides ----------

    [Fact]
    public void AnOverrideWinsOverTheDefault()
    {
        ExceptionStatusCodeMap.Map<OrderNotFoundException>(HttpStatusCode.NotFound);

        HttpStatusCodeHelper.GetStatusCodeForException(new OrderNotFoundException())
            .ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public void AnOverrideCanReclassifyABuiltInException()
    {
        HttpStatusCodeHelper.GetStatusCodeForException(new InvalidOperationException())
            .ShouldBe(HttpStatusCode.InternalServerError);

        ExceptionStatusCodeMap.Map<InvalidOperationException>(HttpStatusCode.Conflict);

        HttpStatusCodeHelper.GetStatusCodeForException(new InvalidOperationException())
            .ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public void AnOverrideOnABaseTypeCoversDerivedTypes()
    {
        ExceptionStatusCodeMap.Map<InvalidOperationException>(HttpStatusCode.Conflict);

        HttpStatusCodeHelper.GetStatusCodeForException(new DomainRuleException())
            .ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public void TheMostDerivedOverrideWins()
    {
        ExceptionStatusCodeMap.Map<InvalidOperationException>(HttpStatusCode.Conflict);
        ExceptionStatusCodeMap.Map<DomainRuleException>(HttpStatusCode.UnprocessableEntity);

        HttpStatusCodeHelper.GetStatusCodeForException(new DomainRuleException())
            .ShouldBe(HttpStatusCode.UnprocessableEntity);
        HttpStatusCodeHelper.GetStatusCodeForException(new InvalidOperationException())
            .ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public void AnOverrideCanBeRemoved()
    {
        ExceptionStatusCodeMap.Map<InvalidOperationException>(HttpStatusCode.Conflict);
        ExceptionStatusCodeMap.Remove<InvalidOperationException>().ShouldBeTrue();

        HttpStatusCodeHelper.GetStatusCodeForException(new InvalidOperationException())
            .ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void ResetRestoresTheDefaults()
    {
        ExceptionStatusCodeMap.Map<InvalidOperationException>(HttpStatusCode.Conflict);
        ExceptionStatusCodeMap.Reset();

        HttpStatusCodeHelper.GetStatusCodeForException(new InvalidOperationException())
            .ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void MappingANonExceptionTypeIsRejected()
    {
        Should.Throw<ArgumentException>(() => ExceptionStatusCodeMap.Map(typeof(string), HttpStatusCode.OK));
        Should.Throw<ArgumentNullException>(() => ExceptionStatusCodeMap.Map(null!, HttpStatusCode.OK));
    }
}
