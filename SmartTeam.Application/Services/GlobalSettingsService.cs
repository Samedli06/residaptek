using AutoMapper;
using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;

namespace SmartTeam.Application.Services;

public class GlobalSettingsService : IGlobalSettingsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    private static readonly Guid SettingsId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public GlobalSettingsService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<GlobalSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _unitOfWork.Repository<GlobalSettings>().GetByIdAsync(SettingsId, cancellationToken);
        
        if (settings == null)
        {
            // This should not happen due to seeding, but as a fallback:
            settings = new GlobalSettings { Id = SettingsId };
            await _unitOfWork.Repository<GlobalSettings>().AddAsync(settings, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return _mapper.Map<GlobalSettingsDto>(settings);
    }

    public async Task<GlobalSettingsDto> UpdateSettingsAsync(UpdateGlobalSettingsDto updateDto, CancellationToken cancellationToken = default)
    {
        var settings = await _unitOfWork.Repository<GlobalSettings>().GetByIdAsync(SettingsId, cancellationToken);
        
        if (settings == null)
        {
            settings = new GlobalSettings { Id = SettingsId };
            await _unitOfWork.Repository<GlobalSettings>().AddAsync(settings, cancellationToken);
        }

        settings.MinimumOrderAmount = updateDto.MinimumOrderAmount;
        settings.IsMinimumOrderAmountEnabled = updateDto.IsMinimumOrderAmountEnabled;
        settings.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<GlobalSettings>().Update(settings);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<GlobalSettingsDto>(settings);
    }
}
