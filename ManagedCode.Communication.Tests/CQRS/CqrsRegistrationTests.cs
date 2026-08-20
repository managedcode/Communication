using System.Linq;
using ManagedCode.Communication.AspNetCore;
using ManagedCode.Communication.AspNetCore.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using CqrsEndpointExtensions = ManagedCode.Communication.AspNetCore.Extensions.CommunicationCqrsEndpointExtensions;
using CqrsMvcExtensions = ManagedCode.Communication.AspNetCore.Extensions.CommunicationCqrsMvcOptionsExtensions;
using CqrsServices = ManagedCode.Communication.AspNetCore.Extensions.CommunicationCqrsServiceCollectionExtensions;

namespace ManagedCode.Communication.Tests.CQRS;

/// <summary>
///     Registration surface: the filter lands in MVC exactly once, options flow through, and the monolithic-package
///     facade behaves identically to the CQRS package it forwards to.
/// </summary>
public class CqrsRegistrationTests
{
    [Test]
    public void AddCommunicationCqrs_RegistersTheActionFilter()
    {
        var services = new ServiceCollection();
        services.AddControllers();
        CqrsServices.AddCommunicationCqrs(services);

        FilterCount(services).ShouldBe(1);
    }

    [Test]
    public void AddCommunicationCqrs_IsIdempotent()
    {
        var services = new ServiceCollection();
        services.AddControllers();

        // Calling the registration twice — or mixing it with its alias — must not double-register the filter,
        // which would run the conversion twice per response.
        CqrsServices.AddCommunicationCqrs(services);
        CqrsServices.AddCommunicationCqrsFilters(services);
        CqrsServices.AddCommunicationCqrs(services);

        FilterCount(services).ShouldBe(1);
    }

    [Test]
    public void AddCommunicationCqrsFiltersOnMvcOptions_IsIdempotent()
    {
        var options = new MvcOptions();

        CqrsMvcExtensions.AddCommunicationCqrsFilters(options);
        CqrsMvcExtensions.AddCommunicationCqrsFilters(options);
        CqrsMvcExtensions.AddCommunicationCqrsFilters(options);

        options.Filters.OfType<TypeFilterAttribute>()
            .Count(filter => filter.ImplementationType == typeof(CqrsResultActionFilter))
            .ShouldBe(1);
    }

    [Test]
    public void AddCommunicationCqrs_ExposesDefaultOptionsWhenNotConfigured()
    {
        var services = new ServiceCollection();
        services.AddControllers();
        CqrsServices.AddCommunicationCqrs(services);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<CqrsStreamServerOptions>>().Value;

        options.AssignSequenceNumbers.ShouldBeTrue();
        options.EnsureTerminalChunk.ShouldBeTrue();
    }

    [Test]
    public void AddCommunicationCqrs_AppliesTheConfigurationCallback()
    {
        var services = new ServiceCollection();
        services.AddControllers();
        CqrsServices.AddCommunicationCqrs(services, options =>
        {
            options.AssignSequenceNumbers = false;
            options.EnsureTerminalChunk = false;
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<CqrsStreamServerOptions>>().Value;

        options.AssignSequenceNumbers.ShouldBeFalse();
        options.EnsureTerminalChunk.ShouldBeFalse();
    }

    [Test]
    public void RegistrationAppliesConfigurationAndRegistersTheFilterOnce()
    {
        var services = new ServiceCollection();
        services.AddControllers();
        CqrsServices.AddCommunicationCqrsFilters(services, options => options.EnsureTerminalChunk = false);

        using var provider = services.BuildServiceProvider();

        FilterCount(services).ShouldBe(1);
        provider.GetRequiredService<IOptions<CqrsStreamServerOptions>>().Value.EnsureTerminalChunk.ShouldBeFalse();
    }

    [Test]
    public void TheActionFilterFallsBackToDefaultsWhenOptionsAreAbsent()
    {
        // Constructed by hand (no DI), as MVC does when the filter is added as an instance.
        Should.NotThrow(() => new CqrsResultActionFilter());
        Should.NotThrow(() => new CqrsResultActionFilter(Options.Create(new CqrsStreamServerOptions())));
    }

    [Test]
    public void TheEndpointFilterRejectsNullOptions()
    {
        Should.Throw<System.ArgumentNullException>(() => new CqrsResultEndpointFilter(null!));
    }

    [Test]
    public void EndpointExtensionsRejectNullBuilders()
    {
        Should.Throw<System.ArgumentNullException>(() =>
            CqrsEndpointExtensions.WithCommunicationCqrsResults((Microsoft.AspNetCore.Builder.RouteHandlerBuilder)null!));
        Should.Throw<System.ArgumentNullException>(() =>
            CqrsEndpointExtensions.WithCommunicationCqrsResults((Microsoft.AspNetCore.Routing.RouteGroupBuilder)null!));
    }

    [Test]
    public void MvcOptionsExtensionRejectsNull()
    {
        Should.Throw<System.ArgumentNullException>(() => CqrsMvcExtensions.AddCommunicationCqrsFilters(null!));
    }

    private static int FilterCount(IServiceCollection services)
    {
        using var provider = services.BuildServiceProvider();
        var mvcOptions = provider.GetRequiredService<IOptions<MvcOptions>>().Value;

        return mvcOptions.Filters.OfType<TypeFilterAttribute>()
            .Count(filter => filter.ImplementationType == typeof(CqrsResultActionFilter));
    }
}
