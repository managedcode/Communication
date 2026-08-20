using System;
using System.Net;
using ManagedCode.Communication.Helpers;
using Shouldly;

namespace ManagedCode.Communication.Tests.Helpers;

/// <summary>
///     Default exception classification and the application overrides on top of it.
/// </summary>
/// <remarks>
///     Not run in parallel with anything else: the override map is process-wide by design, since it is meant to be
///     configured once at startup.
/// </remarks>
[NotInParallel]
public sealed class ExceptionStatusCodeMapTests : IDisposable
{
    public void Dispose()
    {
        ExceptionStatusCodeMap.Reset();
    }

    private sealed class OrderNotFoundException() : Exception("missing");

    private sealed class DomainRuleException() : InvalidOperationException("rule broken");

    // ---------- defaults ----------

    [Test]
    [Arguments(typeof(InvalidOperationException))]
    [Arguments(typeof(NotSupportedException))]
    [Arguments(typeof(InvalidCastException))]
    [Arguments(typeof(NullReferenceException))]
    [Arguments(typeof(IndexOutOfRangeException))]
    public void ServerSideFaultsAre500(Type exceptionType)
    {
        // These say the server reached a state its own code did not allow for. Reporting them as 400 blames the
        // caller and keeps the defect out of every 5xx dashboard.
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;

        HttpStatusCodeHelper.GetStatusCodeForException(exception)
            .ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Test]
    [Arguments(typeof(ArgumentException))]
    [Arguments(typeof(ArgumentNullException))]
    [Arguments(typeof(ArgumentOutOfRangeException))]
    [Arguments(typeof(FormatException))]
    public void MalformedInputIs400(Type exceptionType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;

        HttpStatusCodeHelper.GetStatusCodeForException(exception)
            .ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public void NullIsRejected()
    {
        Should.Throw<ArgumentNullException>(() => HttpStatusCodeHelper.GetStatusCodeForException(null!));
    }

    // ---------- overrides ----------

    [Test]
    public void AnOverrideWinsOverTheDefault()
    {
        ExceptionStatusCodeMap.Map<OrderNotFoundException>(HttpStatusCode.NotFound);

        HttpStatusCodeHelper.GetStatusCodeForException(new OrderNotFoundException())
            .ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public void AnOverrideCanReclassifyABuiltInException()
    {
        HttpStatusCodeHelper.GetStatusCodeForException(new InvalidOperationException())
            .ShouldBe(HttpStatusCode.InternalServerError);

        ExceptionStatusCodeMap.Map<InvalidOperationException>(HttpStatusCode.Conflict);

        HttpStatusCodeHelper.GetStatusCodeForException(new InvalidOperationException())
            .ShouldBe(HttpStatusCode.Conflict);
    }

    [Test]
    public void AnOverrideOnABaseTypeCoversDerivedTypes()
    {
        ExceptionStatusCodeMap.Map<InvalidOperationException>(HttpStatusCode.Conflict);

        HttpStatusCodeHelper.GetStatusCodeForException(new DomainRuleException())
            .ShouldBe(HttpStatusCode.Conflict);
    }

    [Test]
    public void TheMostDerivedOverrideWins()
    {
        ExceptionStatusCodeMap.Map<InvalidOperationException>(HttpStatusCode.Conflict);
        ExceptionStatusCodeMap.Map<DomainRuleException>(HttpStatusCode.UnprocessableEntity);

        HttpStatusCodeHelper.GetStatusCodeForException(new DomainRuleException())
            .ShouldBe(HttpStatusCode.UnprocessableEntity);
        HttpStatusCodeHelper.GetStatusCodeForException(new InvalidOperationException())
            .ShouldBe(HttpStatusCode.Conflict);
    }

    [Test]
    public void AnOverrideCanBeRemoved()
    {
        ExceptionStatusCodeMap.Map<InvalidOperationException>(HttpStatusCode.Conflict);
        ExceptionStatusCodeMap.Remove<InvalidOperationException>().ShouldBeTrue();

        HttpStatusCodeHelper.GetStatusCodeForException(new InvalidOperationException())
            .ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Test]
    public void ResetRestoresTheDefaults()
    {
        ExceptionStatusCodeMap.Map<InvalidOperationException>(HttpStatusCode.Conflict);
        ExceptionStatusCodeMap.Reset();

        HttpStatusCodeHelper.GetStatusCodeForException(new InvalidOperationException())
            .ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Test]
    public void MappingANonExceptionTypeIsRejected()
    {
        Should.Throw<ArgumentException>(() => ExceptionStatusCodeMap.Map(typeof(string), HttpStatusCode.OK));
        Should.Throw<ArgumentNullException>(() => ExceptionStatusCodeMap.Map(null!, HttpStatusCode.OK));
    }
}
