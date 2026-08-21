using System;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;

namespace ManagedCode.Communication.Orleans;

internal sealed class OrleansCommandExecutionStorageValidator(IServiceProvider serviceProvider) : IConfigurationValidator
{
    public void ValidateConfiguration()
    {
        if (serviceProvider.GetKeyedService<IGrainStorage>(
                OrleansCommandExecutionDefaults.IdempotencyStorageName) is not null)
        {
            return;
        }

        throw new OrleansConfigurationException(
            string.Format(
                OrleansCommandExecutionConstants.MissingStorageMessageFormat,
                OrleansCommandExecutionDefaults.IdempotencyStorageName));
    }
}
