using System.Linq;
using ManagedCode.Communication.CQRS.AspNetCore;
using ManagedCode.Communication.CQRS.AspNetCore.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using CoreExtensions = ManagedCode.Communication.CQRS.AspNetCore.Extensions.CommunicationServiceCollectionExtensions;
using CoreMvcExtensions = ManagedCode.Communication.CQRS.AspNetCore.Extensions.MvcOptionsExtensions;
using FacadeEndpointExtensions = ManagedCode.Communication.AspNetCore.Extensions.CommunicationCqrsEndpointExtensions;
using FacadeExtensions = ManagedCode.Communication.AspNetCore.Extensions.CommunicationCqrsServiceCollectionExtensions;
using FacadeMvcExtensions = ManagedCode.Communication.AspNetCore.Extensions.CommunicationCqrsMvcOptionsExtensions;

namespace ManagedCode.Communication.Tests.CQRS;

/// <summary>
///     Registration surface: the filter lands in MVC exactly once, options flow through, and the monolithic-package
///     facade behaves identically to the CQRS package it forwards to.
/// </summary>
public class CqrsRegistrationTests
{
    [Fact]
    public void AddCommunicationCqrs_RegistersTheActionFilter()
    {
        var services = new ServiceCollection();
        services.AddControllers();
        CoreExtensions.AddCommunicationCqrs(services);

        FilterCount(services).ShouldBe(1);
    }

    [Fact]
    public void AddCommunicationCqrs_IsIdempotent()
    {
        var services = new ServiceCollection();
        services.AddControllers();

        // Calling the registration twice — or mixing it with its alias — must not double-register the filter,
        // which would run the conversion twice per response.
        CoreExtensions.AddCommunicationCqrs(services);
        CoreExtensions.AddCommunicationCqrsFilters(services);
        FacadeExtensions.AddCommunicationCqrs(services);

        FilterCount(services).ShouldBe(1);
    }

    [Fact]
    public void AddCommunicationCqrsFiltersOnMvcOptions_IsIdempotent()
    {
        var options = new MvcOptions();

        CoreMvcExtensions.AddCommunicationCqrsFilters(options);
        CoreMvcExtensions.AddCommunicationCqrsFilters(options);
        FacadeMvcExtensions.AddCommunicationCqrsFilters(options);

        options.Filters.OfType<TypeFilterAttribute>()
            .Count(filter => filter.ImplementationType == typeof(CqrsResultActionFilter))
            .ShouldBe(1);
    }

    [Fact]
    public void AddCommunicationCqrs_ExposesDefaultOptionsWhenNotConfigured()
    {
        var services = new ServiceCollection();
        services.AddControllers();
        CoreExtensions.AddCommunicationCqrs(services);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<CqrsStreamServerOptions>>().Value;

        options.AssignSequenceNumbers.ShouldBeTrue();
        options.EnsureTerminalChunk.ShouldBeTrue();
    }

    [Fact]
    public void AddCommunicationCqrs_AppliesTheConfigurationCallback()
    {
        var services = new ServiceCollection();
        services.AddControllers();
        CoreExtensions.AddCommunicationCqrs(services, options =>
        {
            options.AssignSequenceNumbers = false;
            options.EnsureTerminalChunk = false;
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<CqrsStreamServerOptions>>().Value;

        options.AssignSequenceNumbers.ShouldBeFalse();
        options.EnsureTerminalChunk.ShouldBeFalse();
    }

    [Fact]
    public void FacadeRegistration_ForwardsToTheCqrsPackage()
    {
        var services = new ServiceCollection();
        services.AddControllers();
        FacadeExtensions.AddCommunicationCqrsFilters(services, options => options.EnsureTerminalChunk = false);

        using var provider = services.BuildServiceProvider();

        FilterCount(services).ShouldBe(1);
        provider.GetRequiredService<IOptions<CqrsStreamServerOptions>>().Value.EnsureTerminalChunk.ShouldBeFalse();
    }

    [Fact]
    public void TheActionFilterFallsBackToDefaultsWhenOptionsAreAbsent()
    {
        // Constructed by hand (no DI), as MVC does when the filter is added as an instance.
        Should.NotThrow(() => new CqrsResultActionFilter());
        Should.NotThrow(() => new CqrsResultActionFilter(Options.Create(new CqrsStreamServerOptions())));
    }

    [Fact]
    public void TheEndpointFilterRejectsNullOptions()
    {
        Should.Throw<System.ArgumentNullException>(() => new CqrsResultEndpointFilter(null!));
    }

    [Fact]
    public void EndpointExtensionsRejectNullBuilders()
    {
        Should.Throw<System.ArgumentNullException>(() =>
            FacadeEndpointExtensions.WithCommunicationCqrsResults((Microsoft.AspNetCore.Builder.RouteHandlerBuilder)null!));
        Should.Throw<System.ArgumentNullException>(() =>
            FacadeEndpointExtensions.WithCommunicationCqrsResults((Microsoft.AspNetCore.Routing.RouteGroupBuilder)null!));
    }

    [Fact]
    public void MvcOptionsExtensionRejectsNull()
    {
        Should.Throw<System.ArgumentNullException>(() => CoreMvcExtensions.AddCommunicationCqrsFilters(null!));
    }

    private static int FilterCount(IServiceCollection services)
    {
        using var provider = services.BuildServiceProvider();
        var mvcOptions = provider.GetRequiredService<IOptions<MvcOptions>>().Value;

        return mvcOptions.Filters.OfType<TypeFilterAttribute>()
            .Count(filter => filter.ImplementationType == typeof(CqrsResultActionFilter));
    }
}
