using AIBase.Abstractions.UserManagement.Models;
using Microsoft.Extensions.Logging;

namespace AIBase.Core.Grains.Implementations;

public partial class UserGrain
{
    public async Task UpdateSettingsAsync(UserSettings settings)
    {
        if (!state.RecordExists) return;

        state.State = state.State with { Settings = settings };
        await state.WriteStateAsync();
        logger.LogInformation("Updated settings for user {UserId}", state.State.Id);
    }
}