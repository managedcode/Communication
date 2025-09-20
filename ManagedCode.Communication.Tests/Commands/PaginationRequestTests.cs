using System;
using ManagedCode.Communication.Commands;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Commands;

public class PaginationRequestTests
{
    [Fact]
    public void Normalize_ShouldApplyDefaults_WhenTakeIsZero()
    {
        var request = new PaginationRequest(skip: 10, take: 0);

        var normalized = request.Normalize();

        normalized.Take.ShouldBe(PaginationOptions.Default.DefaultPageSize);
        normalized.Skip.ShouldBe(10);
    }

    [Fact]
    public void Normalize_ShouldRespectCustomOptions()
    {
        var options = new PaginationOptions(defaultPageSize: 25, maxPageSize: 40, minPageSize: 10);
        var request = PaginationRequest.Create(skip: -5, take: 100, options);

        var normalized = request;

        normalized.Skip.ShouldBe(0);
        normalized.Take.ShouldBe(40);
    }

    [Fact]
    public void ClampToTotal_ShouldLimitSkip()
    {
        var request = new PaginationRequest(skip: 120, take: 10);

        var clamped = request.ClampToTotal(totalItems: 35);

        clamped.Skip.ShouldBe(34);
        clamped.Take.ShouldBe(1);
    }

    [Fact]
    public void ToSlice_ShouldNormalizeAndClamp()
    {
        var options = new PaginationOptions(defaultPageSize: 20, maxPageSize: 50, minPageSize: 5);
        var request = PaginationRequest.Create(skip: -10, take: 0, options);

        var (start, length) = request.ToSlice(totalItems: 15, options);

        start.ShouldBe(0);
        length.ShouldBe(15);
    }

    [Fact]
    public void FromPage_ShouldNormalizeWithOptions()
    {
        var request = PaginationRequest.FromPage(2, 500, new PaginationOptions(defaultPageSize: 25, maxPageSize: 100, minPageSize: 10));

        request.Skip.ShouldBe(100);
        request.Take.ShouldBe(100);
    }

    [Fact]
    public void PaginationOptions_InvalidArguments_ShouldThrow()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new PaginationOptions(defaultPageSize: 0));
        Should.Throw<ArgumentOutOfRangeException>(() => new PaginationOptions(defaultPageSize: 10, maxPageSize: 0));
        Should.Throw<ArgumentException>(() => new PaginationOptions(defaultPageSize: 50, maxPageSize: 25));
        Should.Throw<ArgumentException>(() => new PaginationOptions(defaultPageSize: 10, maxPageSize: 25, minPageSize: 30));
    }
}
