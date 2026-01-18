using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;

namespace SmartTeam.Application.Services;

public class PromoCodeService : IPromoCodeService
{
    private readonly IUnitOfWork _unitOfWork;

    public PromoCodeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PromoCodeDto> CreatePromoCodeAsync(CreatePromoCodeDto createDto, CancellationToken cancellationToken = default)
    {
        // Check if code already exists
        var existingCode = await _unitOfWork.Repository<PromoCode>()
            .FirstOrDefaultAsync(p => p.Code.ToLower() == createDto.Code.ToLower(), cancellationToken);
        
        if (existingCode != null)
        {
            throw new InvalidOperationException($"Promo code '{createDto.Code}' already exists.");
        }

        // Validate discount percentage
        if (createDto.DiscountPercentage <= 0 || createDto.DiscountPercentage > 100)
        {
            throw new ArgumentException("Discount percentage must be between 0 and 100.");
        }

        var promoCode = new PromoCode
        {
            Id = Guid.NewGuid(),
            Code = createDto.Code.ToUpper(),
            DiscountPercentage = createDto.DiscountPercentage,
            ExpirationDate = createDto.ExpirationDate,
            IsActive = createDto.IsActive,
            UsageLimit = createDto.UsageLimit,
            CurrentUsageCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<PromoCode>().AddAsync(promoCode, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(promoCode);
    }

    public async Task<PromoCodeDto> UpdatePromoCodeAsync(Guid id, UpdatePromoCodeDto updateDto, CancellationToken cancellationToken = default)
    {
        var promoCode = await _unitOfWork.Repository<PromoCode>().GetByIdAsync(id, cancellationToken);
        
        if (promoCode == null)
        {
            throw new InvalidOperationException("Promo code not found.");
        }

        // Update fields if provided
        if (!string.IsNullOrEmpty(updateDto.Code))
        {
            // Check if new code already exists
            var allCodes = await _unitOfWork.Repository<PromoCode>().GetAllAsync(cancellationToken);
            var existingCode = allCodes.FirstOrDefault(p => p.Code.ToLower() == updateDto.Code.ToLower() && p.Id != id);
            
            if (existingCode != null)
            {
                throw new InvalidOperationException($"Promo code '{updateDto.Code}' already exists.");
            }
            
            promoCode.Code = updateDto.Code.ToUpper();
        }

        if (updateDto.DiscountPercentage.HasValue)
        {
            if (updateDto.DiscountPercentage.Value <= 0 || updateDto.DiscountPercentage.Value > 100)
            {
                throw new ArgumentException("Discount percentage must be between 0 and 100.");
            }
            promoCode.DiscountPercentage = updateDto.DiscountPercentage.Value;
        }

        if (updateDto.ExpirationDate.HasValue)
        {
            promoCode.ExpirationDate = updateDto.ExpirationDate.Value;
        }

        if (updateDto.IsActive.HasValue)
        {
            promoCode.IsActive = updateDto.IsActive.Value;
        }

        if (updateDto.UsageLimit.HasValue)
        {
            promoCode.UsageLimit = updateDto.UsageLimit.Value;
        }

        promoCode.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<PromoCode>().Update(promoCode);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(promoCode);
    }

    public async Task DeletePromoCodeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var promoCode = await _unitOfWork.Repository<PromoCode>().GetByIdAsync(id, cancellationToken);
        
        if (promoCode == null)
        {
            throw new InvalidOperationException("Promo code not found.");
        }

        _unitOfWork.Repository<PromoCode>().Remove(promoCode);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PromoCodeDto?> GetPromoCodeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var promoCode = await _unitOfWork.Repository<PromoCode>().GetByIdAsync(id, cancellationToken);
        return promoCode == null ? null : MapToDto(promoCode);
    }

    public async Task<PromoCodeDto?> GetPromoCodeByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var promoCode = await _unitOfWork.Repository<PromoCode>()
            .FirstOrDefaultAsync(p => p.Code.ToLower() == code.ToLower(), cancellationToken);
        
        return promoCode == null ? null : MapToDto(promoCode);
    }

    public async Task<PagedResultDto<PromoCodeListDto>> GetAllPromoCodesAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var allPromoCodes = await _unitOfWork.Repository<PromoCode>().GetAllAsync(cancellationToken);
        
        var totalCount = allPromoCodes.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        
        var promoCodes = allPromoCodes
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PromoCodeListDto
            {
                Id = p.Id,
                Code = p.Code,
                DiscountPercentage = p.DiscountPercentage,
                ExpirationDate = p.ExpirationDate,
                IsActive = p.IsActive,
                CurrentUsageCount = p.CurrentUsageCount,
                UsageLimit = p.UsageLimit
            })
            .ToList();

        return new PagedResultDto<PromoCodeListDto>
        {
            Items = promoCodes,
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = pageNumber < totalPages,
            HasPreviousPage = pageNumber > 1
        };
    }

    public async Task<PromoCodeValidationResultDto> ValidatePromoCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var promoCode = await _unitOfWork.Repository<PromoCode>()
            .FirstOrDefaultAsync(p => p.Code.ToLower() == code.ToLower(), cancellationToken);

        if (promoCode == null)
        {
            return new PromoCodeValidationResultDto
            {
                IsValid = false,
                ErrorMessage = "Invalid promo code."
            };
        }

        if (!promoCode.IsActive)
        {
            return new PromoCodeValidationResultDto
            {
                IsValid = false,
                ErrorMessage = "Promo code is not active."
            };
        }

        if (promoCode.ExpirationDate.HasValue && promoCode.ExpirationDate.Value < DateTime.UtcNow)
        {
            return new PromoCodeValidationResultDto
            {
                IsValid = false,
                ErrorMessage = "Promo code has expired."
            };
        }

        if (promoCode.UsageLimit.HasValue && promoCode.CurrentUsageCount >= promoCode.UsageLimit.Value)
        {
            return new PromoCodeValidationResultDto
            {
                IsValid = false,
                ErrorMessage = "Promo code usage limit exceeded."
            };
        }

        return new PromoCodeValidationResultDto
        {
            IsValid = true,
            PromoCode = MapToDto(promoCode)
        };
    }

    public async Task<PagedResultDto<PromoCodeUsageDto>> GetPromoCodeUsageHistoryAsync(Guid promoCodeId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var allUsages = await _unitOfWork.Repository<PromoCodeUsage>()
            .FindAsync(u => u.PromoCodeId == promoCodeId, cancellationToken);

        var totalCount = allUsages.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var usagesList = new List<PromoCodeUsageDto>();
        
        foreach (var usage in allUsages.OrderByDescending(u => u.UsedAt).Skip((pageNumber - 1) * pageSize).Take(pageSize))
        {
            var promoCode = await _unitOfWork.Repository<PromoCode>().GetByIdAsync(usage.PromoCodeId, cancellationToken);
            User? user = null;
            if (usage.UserId.HasValue)
            {
                user = await _unitOfWork.Repository<User>().GetByIdAsync(usage.UserId.Value, cancellationToken);
            }

            usagesList.Add(new PromoCodeUsageDto
            {
                Id = usage.Id,
                PromoCodeId = usage.PromoCodeId,
                PromoCodeName = promoCode?.Code ?? "",
                UserId = usage.UserId,
                UserName = user != null ? $"{user.FirstName} {user.LastName}" : null,
                CartId = usage.CartId,
                DiscountAmount = usage.DiscountAmount,
                OrderTotal = usage.OrderTotal,
                UsedAt = usage.UsedAt
            });
        }

        return new PagedResultDto<PromoCodeUsageDto>
        {
            Items = usagesList,
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = pageNumber < totalPages,
            HasPreviousPage = pageNumber > 1
        };
    }

    public async Task RecordPromoCodeUsageAsync(Guid promoCodeId, Guid? userId, Guid? cartId, decimal discountAmount, decimal orderTotal, CancellationToken cancellationToken = default)
    {
        var promoCode = await _unitOfWork.Repository<PromoCode>().GetByIdAsync(promoCodeId, cancellationToken);
        
        if (promoCode == null)
        {
            throw new InvalidOperationException("Promo code not found.");
        }

        var usage = new PromoCodeUsage
        {
            Id = Guid.NewGuid(),
            PromoCodeId = promoCodeId,
            UserId = userId,
            CartId = cartId,
            DiscountAmount = discountAmount,
            OrderTotal = orderTotal,
            UsedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<PromoCodeUsage>().AddAsync(usage, cancellationToken);
        
        // Increment usage count
        promoCode.CurrentUsageCount++;
        _unitOfWork.Repository<PromoCode>().Update(promoCode);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static PromoCodeDto MapToDto(PromoCode promoCode)
    {
        return new PromoCodeDto
        {
            Id = promoCode.Id,
            Code = promoCode.Code,
            DiscountPercentage = promoCode.DiscountPercentage,
            ExpirationDate = promoCode.ExpirationDate,
            IsActive = promoCode.IsActive,
            UsageLimit = promoCode.UsageLimit,
            CurrentUsageCount = promoCode.CurrentUsageCount,
            CreatedAt = promoCode.CreatedAt,
            UpdatedAt = promoCode.UpdatedAt
        };
    }
}
