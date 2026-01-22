using SmartTeam.Application.DTOs;

namespace SmartTeam.Application.Services;

public interface IOrderService
{
    Task<OrderDto> CreateOrderFromCartAsync(Guid userId, CreateOrderDto createOrderDto, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderListDto>> GetUserOrdersAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderListDto>> GetAllOrdersAsync(CancellationToken cancellationToken = default);
    Task<OrderDto> UpdateOrderStatusAsync(Guid orderId, UpdateOrderStatusDto updateDto, CancellationToken cancellationToken = default);
}
