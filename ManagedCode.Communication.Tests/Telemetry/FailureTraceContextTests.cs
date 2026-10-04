using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ManagedCode.Communication.Telemetry;
using Shouldly;

namespace ManagedCode.Communication.Tests.Telemetry;

[NotInParallel]
public sealed class FailureTraceContextTests
{
    private const string NativeSourceName = "failure-context-native-source";
    private const string NativeOperation = "POST /oauth/token";
    private const string NativeStatusTag = "http.response.status_code";
    private const string FailureEventName = "communication.result.failure";
    private const string FailureCode = "invalid_grant";
    private const string FailureTitle = "Bad Request";
    private const string FailureDetail = "OAuth refresh token is invalid or expired.";
    private const string SecretExtension = "access_token";
    private const string SecretValue = "private-test-token";

    [Test]
    [Arguments(200, ActivityKind.Server, ActivityStatusCode.Ok)]
    [Arguments(400, ActivityKind.Server, ActivityStatusCode.Error)]
    [Arguments(400, ActivityKind.Client, ActivityStatusCode.Error)]
    public void Failure_ShouldStayOnNativeOperationWithoutCreatingDependencyOrReplacingResponseCode(
        int nativeStatus, ActivityKind kind, ActivityStatusCode finalStatus)
    {
        var stopped = new List<Activity>();
        using var listener = CreateListener(stopped);
        ActivitySource.AddActivityListener(listener);
        using var source = new ActivitySource(NativeSourceName);
        using var parent = source.StartActivity(NativeOperation, kind);
        parent.ShouldNotBeNull();
        parent.SetTag(NativeStatusTag, nativeStatus);
        parent.SetStatus(finalStatus);
        var problem = Problem.Create(FailureTitle, FailureDetail, 400);
        problem.ErrorCode = FailureCode;
        problem.Extensions[SecretExtension] = SecretValue;

        var result = Result<int>.Fail(problem);
        Result.Fail(result.Problem!).IsFailed.ShouldBeTrue();

        Activity.Current.ShouldBeSameAs(parent);
        stopped.ShouldBeEmpty();
        parent.Status.ShouldBe(finalStatus);
        parent.GetTagItem(NativeStatusTag).ShouldBe(nativeStatus);
        parent.GetTagItem(CommunicationTelemetry.ProblemStatusTag).ShouldBeNull();
        var failure = parent.Events.Single(item => item.Name == FailureEventName);
        failure.Tags.Single(tag => tag.Key == CommunicationTelemetry.ProblemStatusTag).Value.ShouldBe(400);
        failure.Tags.Single(tag => tag.Key == CommunicationTelemetry.ProblemErrorCodeTag).Value.ShouldBe(FailureCode);
        failure.Tags.Single(tag => tag.Key == CommunicationTelemetry.ProblemTitleTag).Value.ShouldBe(FailureTitle);
        failure.Tags.Single(tag => tag.Key == CommunicationTelemetry.ProblemDetailTag).Value.ShouldBe(FailureDetail);
        failure.Tags.ShouldNotContain(tag => tag.Key == SecretExtension || Equals(tag.Value, SecretValue));
    }

    [Test]
    public void FailureWithoutCurrentOperation_ShouldNotCreateSyntheticRootTrace()
    {
        var stopped = new List<Activity>();
        using var listener = CreateListener(stopped);
        ActivitySource.AddActivityListener(listener);
        var previous = Activity.Current;
        try
        {
            Activity.Current = null;
            Result.Fail(FailureTitle, FailureDetail).IsFailed.ShouldBeTrue();
            Activity.Current.ShouldBeNull();
            stopped.ShouldBeEmpty();
        }
        finally
        {
            Activity.Current = previous;
        }
    }

    private static ActivityListener CreateListener(List<Activity> stopped) => new()
    {
        ShouldListenTo = source => source.Name == CommunicationTelemetry.SourceName || source.Name == NativeSourceName,
        Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        ActivityStopped = stopped.Add
    };
}
