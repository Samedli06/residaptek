using SmartTeam.Application.DTOs;

namespace SmartTeam.Application.Services;

public interface IGlobalSettingsService
{
    Task<GlobalSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<GlobalSettingsDto> UpdateSettingsAsync(UpdateGlobalSettingsDto updateDto, CancellationToken cancellationToken = default);
}
