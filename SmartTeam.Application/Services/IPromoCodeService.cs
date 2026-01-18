using SmartTeam.Application.DTOs;

namespace SmartTeam.Application.Services;

public interface IPromoCodeService
{
    // Admin operations
    Task<PromoCodeDto> CreatePromoCodeAsync(CreatePromoCodeDto createDto, CancellationToken cancellationToken = default);
    Task<PromoCodeDto> UpdatePromoCodeAsync(Guid id, UpdatePromoCodeDto updateDto, CancellationToken cancellationToken = default);
    Task DeletePromoCodeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PromoCodeDto?> GetPromoCodeByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PromoCodeDto?> GetPromoCodeByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<PagedResultDto<PromoCodeListDto>> GetAllPromoCodesAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    
    // Validation
    Task<PromoCodeValidationResultDto> ValidatePromoCodeAsync(string code, CancellationToken cancellationToken = default);
    
    // Usage tracking
    Task<PagedResultDto<PromoCodeUsageDto>> GetPromoCodeUsageHistoryAsync(Guid promoCodeId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task RecordPromoCodeUsageAsync(Guid promoCodeId, Guid? userId, Guid? cartId, decimal discountAmount, decimal orderTotal, CancellationToken cancellationToken = default);
}
