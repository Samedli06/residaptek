using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Application.Services;

public interface ICartService
{
    Task<CartDto> GetUserCartAsync(Guid? userId, CancellationToken cancellationToken = default);
    Task<CartDto> AddToCartAsync(Guid? userId, AddToCartDto addToCartDto, CancellationToken cancellationToken = default);
    Task<CartDto> UpdateCartItemAsync(Guid? userId, Guid cartItemId, UpdateCartItemDto updateCartItemDto, CancellationToken cancellationToken = default);
    Task<CartDto> RemoveFromCartAsync(Guid? userId, Guid cartItemId, CancellationToken cancellationToken = default);
    Task<bool> ClearCartAsync(Guid? userId, CancellationToken cancellationToken = default);
    Task<WhatsAppLinkDto> GenerateWhatsAppOrderAsync(Guid? userId, WhatsAppOrderDto orderDto, CancellationToken cancellationToken = default);
    Task<WhatsAppLinkDto> GenerateQuickWhatsAppOrderAsync(Guid? userId, QuickOrderDto quickOrderDto, CancellationToken cancellationToken = default);
    Task<int> GetCartCountAsync(Guid? userId, CancellationToken cancellationToken = default);
    
    // Promo code methods
    Task<CartDto> ApplyPromoCodeAsync(Guid? userId, string promoCode, CancellationToken cancellationToken = default);
    Task<CartDto> RemovePromoCodeAsync(Guid? userId, CancellationToken cancellationToken = default);
}

public interface IWhatsAppService
{
    string GenerateWhatsAppUrl(string phoneNumber, string message);
    string FormatOrderMessage(WhatsAppOrderDto orderDto);
}
