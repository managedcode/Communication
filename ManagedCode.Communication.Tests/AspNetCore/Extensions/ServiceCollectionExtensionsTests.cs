using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using ManagedCode.Communication.Tests.Common.TestApp;
using Xunit;

namespace ManagedCode.Communication.Tests.AspNetCore.Extensions;

[Collection(nameof(TestClusterApplication))]
public class ServiceCollectionExtensionsTests(TestClusterApplication app)
{
    [Fact]
    public async Task Communication_Should_Handle_Successful_Result()
    {
        // Arrange
        var client = app.CreateClient();

        // Act
        var response = await client.GetAsync("/test/result-success");

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("Test Success");
    }

    [Fact]
    public async Task Communication_Should_Handle_Failed_Result()
    {
        // Arrange
        var client = app.CreateClient();

        // Act
        var response = await client.GetAsync("/test/result-failure");

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("400");
    }

    [Fact]
    public async Task Communication_Should_Handle_NotFound_Result()
    {
        // Arrange
        var client = app.CreateClient();

        // Act
        var response = await client.GetAsync("/test/result-notfound");

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("404");
    }

    [Fact]
    public async Task Communication_Should_Handle_Collection_Results()
    {
        // Arrange
        var client = app.CreateClient();

        // Act
        var response = await client.GetAsync("/test/collection-success");

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("collection");
        content.ShouldContain("totalItems");
    }

    [Fact]
    public async Task Communication_Should_Handle_Empty_Collections()
    {
        // Arrange
        var client = app.CreateClient();

        // Act
        var response = await client.GetAsync("/test/collection-empty");

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("collection");
    }

    [Fact]
    public async Task Communication_Should_Handle_Enum_Errors()
    {
        // Arrange
        var client = app.CreateClient();

        // Act
        var response = await client.GetAsync("/test/enum-error");

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("400");
    }

    [Fact]
    public async Task Communication_Should_Handle_Valid_Model_Validation()
    {
        // Arrange
        var client = app.CreateClient();
        var validModel = "{\"name\":\"John Doe\",\"email\":\"john@example.com\",\"age\":30}";

        // Act
        var response = await client.PostAsync("/test/validate",
            new StringContent(validModel, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("Validation passed");
    }

    [Fact]
    public async Task Communication_Should_Reject_Invalid_Model()
    {
        // Arrange
        var client = app.CreateClient();
        var invalidModel = "{\"name\":\"\",\"email\":\"invalid\",\"age\":-1}";

        // Act
        var response = await client.PostAsync("/test/validate",
            new StringContent(invalidModel, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Communication_Should_Handle_Custom_Problems()
    {
        // Arrange
        var client = app.CreateClient();

        // Act
        var response = await client.GetAsync("/test/custom-problem");

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Conflict);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("409");
    }

    [Fact]
    public async Task Communication_Should_Handle_Exceptions()
    {
        // Arrange
        var client = app.CreateClient();

        // Act
        var response = await client.GetAsync("/test/throw-exception");

        // Assert - Could be 400 or 500 depending on how ASP.NET handles it
        response.IsSuccessStatusCode.ShouldBeFalse();
    }
}
