using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Application.Services;

public interface IOrderService
{
    Task<OrderDto> CreateOrderFromCartAsync(Guid userId, CreateOrderDto createOrderDto, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderListDto>> GetUserOrdersAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PagedResultDto<OrderListDto>> GetAllOrdersAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResultDto<OrderListDto>> SearchOrdersByNameAsync(string customerName, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderDto>> GetOrdersForExportAsync(DateTime? fromDate, DateTime? toDate, OrderStatus? status, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetOrderWithDetailsAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderDto> UpdateOrderStatusAsync(Guid orderId, UpdateOrderStatusDto updateDto, CancellationToken cancellationToken = default);
}
