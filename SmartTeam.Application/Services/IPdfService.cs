using SmartTeam.Application.DTOs;

namespace SmartTeam.Application.Services;

public interface IPdfService
{
    byte[] GenerateOrderReceipt(OrderDto order);
    byte[] GenerateBulkOrderReceipts(IEnumerable<OrderDto> orders, DateTime? fromDate, DateTime? toDate);
}
