using System.Linq;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Commands;
using Shouldly;

namespace ManagedCode.Communication.Tests.CollectionResults;

public class CollectionResultPaginationTests
{
    [Test]
    public void Succeed_WithPaginationRequest_ShouldPopulateMetadata()
    {
        var items = new[] { 1, 2, 3 };
        var request = PaginationRequest.Create(skip: 3, take: 3);

        var result = CollectionResult<int>.Succeed(items, request, totalItems: 10);

        result.IsSuccess.ShouldBeTrue();
        result.PageNumber.ShouldBe(2);
        result.PageSize.ShouldBe(request.Take);
        result.TotalItems.ShouldBe(10);
        result.TotalPages.ShouldBe(4);
    }

    [Test]
    public void Succeed_WithPartialLastPage_PreservesNormalizedPagingMetadata()
    {
        int[] items = [51, 52, 53];
        var request = PaginationRequest.FromPage(3, 25);

        var arrayResult = CollectionResult<int>.Succeed(items, request, totalItems: 53);
        var enumerableResult = CollectionResult<int>.Succeed(items.Select(static item => item), request, totalItems: 53);

        arrayResult.PageNumber.ShouldBe(3);
        arrayResult.PageSize.ShouldBe(25);
        arrayResult.TotalPages.ShouldBe(3);
        arrayResult.Collection.ShouldBeSameAs(items);
        enumerableResult.PageNumber.ShouldBe(3);
        enumerableResult.PageSize.ShouldBe(25);
        enumerableResult.TotalPages.ShouldBe(3);
        enumerableResult.Collection.ShouldBe(items);
        request.ToSlice(53).ShouldBe((50, 3));
    }

    [Test]
    public void Succeed_WithOptions_ShouldClampPageSize()
    {
        var items = new[] { 1, 2, 3, 4, 5 };
        var options = new PaginationOptions(defaultPageSize: 5, maxPageSize: 5, minPageSize: 2);
        var request = new PaginationRequest(skip: 0, take: 10);

        var result = CollectionResult<int>.Succeed(items, request, totalItems: 5, options);

        result.PageSize.ShouldBe(5);
        result.PageNumber.ShouldBe(1);
        result.TotalPages.ShouldBe(1);
    }
}
