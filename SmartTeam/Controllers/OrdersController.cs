using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;
using SmartTeam.Domain.Entities;
using System.Security.Claims;

namespace SmartTeam.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IPdfService _pdfService;

    public OrdersController(IOrderService orderService, IPdfService pdfService)
    {
        _orderService = orderService;
        _pdfService = pdfService;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto createOrderDto, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var order = await _orderService.CreateOrderFromCartAsync(userId, createOrderDto, cancellationToken);
            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Log ex
            return StatusCode(500, new { message = "An error occurred while creating the order." });
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<OrderListDto>>> GetUserOrders(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var orders = await _orderService.GetUserOrdersAsync(userId, cancellationToken);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<OrderDto>> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderByIdAsync(id, cancellationToken);
        
        if (order == null) return NotFound();

        // Check if user is allowed to view this order
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (order.UserId != userId && userRole != "Admin")
        {
            return Forbid();
        }

        return Ok(order);
    }

    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResultDto<OrderListDto>>> GetAllOrders(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetAllOrdersAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("search")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResultDto<OrderListDto>>> SearchOrders(
        [FromQuery] string customerName, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerName))
        {
            return BadRequest(new { message = "Customer name is required" });
        }
        
        var result = await _orderService.SearchOrdersByNameAsync(customerName, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderDto>> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusDto updateDto, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderService.UpdateOrderStatusAsync(id, updateDto, cancellationToken);
            return Ok(order);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/pdf")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetOrderPdf(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderWithDetailsAsync(id, cancellationToken);
        if (order == null) return NotFound();

        var pdfBytes = _pdfService.GenerateOrderReceipt(order);
        return File(pdfBytes, "application/pdf", $"qaimə-{order.OrderNumber}.pdf");
    }

    [HttpGet("export/pdf")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportOrdersPdf(
        [FromQuery] DateTime? fromDate, 
        [FromQuery] DateTime? toDate, 
        [FromQuery] OrderStatus? status, 
        CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetOrdersForExportAsync(fromDate, toDate, status, cancellationToken);
        
        if (!orders.Any())
            return NotFound(new { message = "No orders found for the specified criteria." });

        var pdfBytes = _pdfService.GenerateBulkOrderReceipts(orders, fromDate, toDate);
        return File(pdfBytes, "application/pdf", $"sifarişlər-export-{DateTime.Now:yyyyMMdd}.pdf");
    }

    /// <summary>
    /// Delete an order by ID (Admin only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteOrder(Guid id, CancellationToken cancellationToken)
    {
        var result = await _orderService.DeleteOrderAsync(id, cancellationToken);
        
        if (!result)
        {
            return NotFound(new { message = "Order not found." });
        }

        return Ok(new { message = "Order deleted successfully." });
    }
}
