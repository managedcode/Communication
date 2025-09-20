using System;
using ManagedCode.Communication.Commands;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Commands;

public class PaginationCommandTests
{
    [Fact]
    public void Create_WithSkipAndTake_ShouldPopulateProperties()
    {
        var command = PaginationCommand.Create(10, 5);

        command.CommandType
            .ShouldBe("Pagination");
        command.Skip
            .ShouldBe(10);
        command.Take
            .ShouldBe(5);
        command.PageNumber
            .ShouldBe(3);
        command.PageSize
            .ShouldBe(5);
        command.Value
            .ShouldNotBeNull();
        command.Value!.Skip
            .ShouldBe(10);
    }

    [Fact]
    public void Create_WithCommandId_ShouldRespectIdentifier()
    {
        var id = Guid.NewGuid();

        var command = PaginationCommand.Create(id, 12, 6);

        command.CommandId
            .ShouldBe(id);
        command.Skip
            .ShouldBe(12);
        command.Take
            .ShouldBe(6);
    }

    [Fact]
    public void Create_WithNegativeSkip_ShouldThrow()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PaginationCommand.Create(-1, 10));
    }

    [Fact]
    public void Create_WithNegativeTake_ShouldThrow()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PaginationCommand.Create(0, -1));
    }

    [Fact]
    public void FromPage_WithValidParameters_ShouldCalculateSkipAndTake()
    {
        var command = PaginationCommand.FromPage(3, 20);

        command.Skip
            .ShouldBe(40);
        command.Take
            .ShouldBe(20);
        command.PageNumber
            .ShouldBe(3);
        command.PageSize
            .ShouldBe(20);
    }

    [Fact]
    public void PaginationRequest_FromPage_InvalidInput_ShouldThrow()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PaginationRequest.FromPage(0, 10));
        Should.Throw<ArgumentOutOfRangeException>(() => PaginationRequest.FromPage(1, 0));
    }
}
