using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;

namespace SmartTeam.Controllers;

/// <summary>
/// Admin-only Product Purchase Expense tracking controller.
/// Completely isolated from sales, profit, and statistics modules.
/// Route prefix: api/v1/admin/purchase-expenses
/// </summary>
[ApiController]
[Route("api/v1/admin/purchase-expenses")]
[Authorize(Roles = "Admin")]
public class ProductPurchaseExpensesController : ControllerBase
{
    private readonly IProductPurchaseExpenseService _expenseService;

    public ProductPurchaseExpensesController(IProductPurchaseExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>
    /// POST api/v1/admin/purchase-expenses
    /// Records a new product purchase expense.
    /// TotalExpense is automatically calculated as Quantity × UnitPurchasePrice.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductPurchaseExpenseDto), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<ProductPurchaseExpenseDto>> Create(
        [FromBody] CreateProductPurchaseExpenseDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var expense = await _expenseService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = expense.Id }, expense);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while creating the expense record." });
        }
    }

    /// <summary>
    /// GET api/v1/admin/purchase-expenses
    /// Returns all expense entries ordered by purchase date descending.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductPurchaseExpenseDto>), 200)]
    public async Task<ActionResult<IEnumerable<ProductPurchaseExpenseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        try
        {
            var expenses = await _expenseService.GetAllAsync(cancellationToken);
            return Ok(expenses);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving expense records." });
        }
    }

    /// <summary>
    /// GET api/v1/admin/purchase-expenses/{id}
    /// Returns a single expense entry — invoice-style view.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductPurchaseExpenseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ProductPurchaseExpenseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var expense = await _expenseService.GetByIdAsync(id, cancellationToken);
            if (expense == null)
                return NotFound(new { message = "Expense record not found." });

            return Ok(expense);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving the expense record." });
        }
    }

    /// <summary>
    /// GET api/v1/admin/purchase-expenses/summary
    /// Returns aggregated expense totals: today, this month, this year, and all-time.
    /// Calculations based on PurchaseDate.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ProductPurchaseExpenseSummaryDto), 200)]
    public async Task<ActionResult<ProductPurchaseExpenseSummaryDto>> GetSummary(
        CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _expenseService.GetSummaryAsync(cancellationToken);
            return Ok(summary);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while calculating the expense summary." });
        }
    }

    /// <summary>
    /// GET api/v1/admin/purchase-expenses/date-range?startDate=2026-01-01&amp;endDate=2026-03-31
    /// Returns expense totals for a custom date range.
    /// Matched against PurchaseDate.
    /// </summary>
    [HttpGet("date-range")]
    [ProducesResponseType(typeof(ProductPurchaseExpenseByDateRangeDto), 200)]
    public async Task<ActionResult<ProductPurchaseExpenseByDateRangeDto>> GetByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        if (startDate > endDate)
            return BadRequest(new { message = "startDate must be before or equal to endDate." });

        try
        {
            var result = await _expenseService.GetByDateRangeAsync(startDate, endDate, cancellationToken);
            return Ok(result);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving expenses for the date range." });
        }
    }

    /// <summary>
    /// DELETE api/v1/admin/purchase-expenses/{id}
    /// Permanently deletes an expense entry.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _expenseService.DeleteAsync(id, cancellationToken);
            if (!deleted)
                return NotFound(new { message = "Expense record not found." });

            return NoContent();
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while deleting the expense record." });
        }
    }
}
