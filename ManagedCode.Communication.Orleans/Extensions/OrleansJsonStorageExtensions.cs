using System;
using System.Linq;
using System.Text.Json;
using ManagedCode.Communication.Orleans.Converters;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using Orleans.Serialization;
using Orleans.Serialization.Configuration;

namespace ManagedCode.Communication.Orleans.Extensions;

/// <summary>Configures the native Orleans JSON storage serializer for Communication results.</summary>
public static class OrleansJsonStorageExtensions
{
    /// <summary>Preserves Result flags, typed values and Problems in native Orleans JSON state.</summary>
    public static IServiceCollection AddCommunicationOrleansJsonStorage(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.Configure<TypeManifestOptions>(options => options.AddAllowedType(typeof(JsonElement)));
        services.Configure<OrleansJsonSerializerOptions>(options =>
        {
            if (options.JsonSerializerSettings.ContractResolver is not CommunicationJsonElementContractResolver)
            {
                options.JsonSerializerSettings.ContractResolver = new CommunicationJsonElementContractResolver(
                    options.JsonSerializerSettings.ContractResolver ?? new DefaultContractResolver());
            }
            if (!options.JsonSerializerSettings.Converters.OfType<CommunicationResultJsonConverter>().Any())
            {
                options.JsonSerializerSettings.Converters.Insert(0, new CommunicationResultJsonConverter());
            }
        });
        return services;
    }
}
