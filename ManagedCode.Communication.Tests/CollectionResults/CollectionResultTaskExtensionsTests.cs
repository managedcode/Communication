using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.CollectionResults.Extensions;
using ManagedCode.Communication.Tests.TestHelpers;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.CollectionResults;

public class CollectionResultTaskExtensionsTests
{
    [Fact]
    public async Task AsTask_ReturnsSameCollectionResult()
    {
        var original = CollectionResult<int>.Succeed(new[] { 1, 2, 3 });

        var result = await original.AsTask();

        result.IsSuccess.ShouldBeTrue();
        result.Collection.ShouldBeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task AsValueTask_ReturnsSameCollectionResult()
    {
        var problem = Problem.Create("failure", "oops");
        var original = CollectionResult<string>.Fail(problem);

        var result = await original.AsValueTask();

        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldBeSameAs(problem);
    }
}
