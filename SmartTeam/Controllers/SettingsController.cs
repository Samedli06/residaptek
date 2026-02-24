using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;

namespace SmartTeam.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly IGlobalSettingsService _settingsService;

    public SettingsController(IGlobalSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<ActionResult<GlobalSettingsDto>> GetSettings(CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetSettingsAsync(cancellationToken);
        return Ok(settings);
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GlobalSettingsDto>> UpdateSettings(UpdateGlobalSettingsDto updateDto, CancellationToken cancellationToken)
    {
        var settings = await _settingsService.UpdateSettingsAsync(updateDto, cancellationToken);
        return Ok(settings);
    }
}
