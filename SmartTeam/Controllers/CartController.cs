using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;
using System.Security.Claims;
using System.Linq;
using System.Net;

namespace SmartTeam.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    /// <summary>
    /// Get current user's cart
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CartDto>> GetCart(CancellationToken cancellationToken)
    {
        Guid? userId = null;
        
        // Try to get user ID if authenticated, but don't require it
        try
        {
            // Check if User is authenticated first
            if (User.Identity?.IsAuthenticated == true)
            {
                userId = GetCurrentUserId();
            }
        }
        catch (Exception)
        {
            // User is not authenticated, continue without user ID
        }

        // For anonymous users, return empty cart
        if (!userId.HasValue)
        {
            return Ok(new CartDto
            {
                Id = Guid.Empty,
                UserId = Guid.Empty,
                Items = new List<CartItemDto>(),
                TotalAmount = 0,
                TotalPriceBeforeDiscount = 0,
                TotalDiscount = 0,
                TotalQuantity = 0,
                CreatedAt = DateTime.UtcNow
            });
        }

        var cart = await _cartService.GetUserCartAsync(userId, cancellationToken);
        return Ok(cart);
    }

    /// <summary>
    /// Add product to cart
    /// </summary>
    [HttpPost("items")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CartDto>> AddToCart([FromBody] AddToCartRequestDto? requestDto, CancellationToken cancellationToken)
    {
        try
        {
            if (requestDto == null)
            {
                return BadRequest("Request body is required.");
            }

            // Convert string ProductId to Guid - try multiple formats and cleaning
            var productIdString = requestDto.ProductId?.Trim();
            
            // Clean invisible characters
            if (!string.IsNullOrEmpty(productIdString))
            {
                productIdString = productIdString.Replace("\u200E", "")
                                               .Replace("\u200F", "")
                                               .Replace("\u202A", "")
                                               .Replace("\u202B", "")
                                               .Replace("\u202C", "")
                                               .Replace("\u202D", "")
                                               .Replace("\u202E", "");
                
                // If it looks like it has an extra character at the end (e.g. 37 chars)
                if (productIdString.Length == 37)
                {
                    var validChars = productIdString.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray();
                    if (validChars.Length > 36)
                    {
                        // Take only the first 36 valid chars if it's still too long
                        productIdString = new string(validChars.Take(36).ToArray());
                    }
                    else
                    {
                        productIdString = new string(validChars);
                    }
                }
            }
            
            if (!Guid.TryParse(productIdString, out var productId))
            {
                // Try alternate formats
                if (!Guid.TryParseExact(productIdString, "D", out productId) &&
                    !Guid.TryParseExact(productIdString, "N", out productId) &&
                    !Guid.TryParseExact(productIdString, "B", out productId) &&
                    !Guid.TryParseExact(productIdString, "P", out productId))
                {
                    return BadRequest($"Invalid ProductId format: '{requestDto.ProductId}'");
                }
            }

            var addToCartDto = new AddToCartDto
            {
                ProductId = productId,
                Quantity = requestDto.Quantity
            };

            Guid? userId = null;
            
            // Try to get user ID if authenticated, but don't require it
            try
            {
                // Check if User is authenticated first
                if (User.Identity?.IsAuthenticated == true)
                {
                    userId = GetCurrentUserId();
                }
            }
            catch (Exception)
            {
                // User is not authenticated, continue without user ID
            }

            var cart = await _cartService.AddToCartAsync(userId, addToCartDto, cancellationToken);
            return Ok(cart);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update cart item quantity
    /// </summary>
    [HttpPut("items/{cartItemId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> UpdateCartItem(Guid cartItemId, [FromBody] UpdateCartItemDto updateCartItemDto, CancellationToken cancellationToken)
    {
        try
        {
            Guid? userId = null;
            try { if (User.Identity?.IsAuthenticated == true) userId = GetCurrentUserId(); } catch { }
            
            var cart = await _cartService.UpdateCartItemAsync(userId, cartItemId, updateCartItemDto, cancellationToken);
            return Ok(cart);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Remove item from cart
    /// </summary>
    [HttpDelete("items/{cartItemId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> RemoveFromCart(Guid cartItemId, CancellationToken cancellationToken)
    {
        try
        {
            Guid? userId = null;
            try { if (User.Identity?.IsAuthenticated == true) userId = GetCurrentUserId(); } catch { }
            
            var cart = await _cartService.RemoveFromCartAsync(userId, cartItemId, cancellationToken);
            return Ok(cart);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Clear all items from cart
    /// </summary>
    [HttpDelete]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ClearCart(CancellationToken cancellationToken)
    {
        Guid? userId = null;
        try { if (User.Identity?.IsAuthenticated == true) userId = GetCurrentUserId(); } catch { }
        
        await _cartService.ClearCartAsync(userId, cancellationToken);
        return Ok(new { message = "Cart cleared successfully" });
    }

    /// <summary>
    /// Generate WhatsApp order link (Buy Now)
    /// </summary>
    [HttpPost("whatsapp-order")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(WhatsAppLinkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WhatsAppLinkDto>> GenerateWhatsAppOrder([FromBody] WhatsAppOrderDto orderDto, CancellationToken cancellationToken)
    {
        try
        {
            Guid? userId = null;
            
            try
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    userId = GetCurrentUserId();
                }
            }
            catch (Exception)
            {
            }
            
            if (userId.HasValue && (string.IsNullOrEmpty(orderDto.CustomerName) || string.IsNullOrEmpty(orderDto.CustomerPhone)))
            {
                var userClaims = User.Claims;
                orderDto.CustomerName = $"{userClaims.FirstOrDefault(c => c.Type == "FirstName")?.Value} {userClaims.FirstOrDefault(c => c.Type == "LastName")?.Value}".Trim();
                orderDto.CustomerPhone = userClaims.FirstOrDefault(c => c.Type == ClaimTypes.MobilePhone)?.Value ?? "";
            }

            var whatsAppLink = await _cartService.GenerateWhatsAppOrderAsync(userId, orderDto, cancellationToken);
            return Ok(whatsAppLink);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Generate WhatsApp order link for a single product (Quick Order / Bir Klikle Al)
    /// </summary>
    [HttpPost("quick-order")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(WhatsAppLinkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WhatsAppLinkDto>> GenerateQuickOrder([FromBody] QuickOrderRequestDto? requestDto, CancellationToken cancellationToken)
    {
        try
        {
            // Debug: Check if the DTO is null
            if (requestDto == null)
            {
                return BadRequest(new { error = "Request body is null or invalid." });
            }

            // Debug: Log the raw ProductId string
            Console.WriteLine($"Raw ProductId string: '{requestDto.ProductId}'");
            Console.WriteLine($"ProductId length: {requestDto.ProductId?.Length}");
            Console.WriteLine($"ProductId bytes: {string.Join(", ", requestDto.ProductId?.Select(c => (int)c) ?? new int[0])}");
            
            // Show each character with its position
            if (!string.IsNullOrEmpty(requestDto.ProductId))
            {
                for (int i = 0; i < requestDto.ProductId.Length; i++)
                {
                    Console.WriteLine($"Position {i}: '{requestDto.ProductId[i]}' (ASCII: {(int)requestDto.ProductId[i]})");
                }
            }

            // Convert string ProductId to Guid - try multiple formats
            var productIdString = requestDto.ProductId?.Trim();
            
            // If the string is 37 characters, try to find and remove the extra character
            if (productIdString?.Length == 37)
            {
                // Try removing common problematic characters
                productIdString = productIdString.Replace("\u200E", "").Replace("\u200F", "").Replace("\u202A", "").Replace("\u202B", "").Replace("\u202C", "").Replace("\u202D", "").Replace("\u202E", "");
                
                // If still 37 characters, try to extract only valid GUID characters
                if (productIdString.Length == 37)
                {
                    // Extract only alphanumeric characters and hyphens
                    var validChars = productIdString.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray();
                    productIdString = new string(validChars);
                }
            }
            
            if (!Guid.TryParse(productIdString, out var productId))
            {
                // Try parsing with different formats
                if (Guid.TryParseExact(productIdString, "D", out productId) ||
                    Guid.TryParseExact(productIdString, "N", out productId) ||
                    Guid.TryParseExact(productIdString, "B", out productId) ||
                    Guid.TryParseExact(productIdString, "P", out productId))
                {
                    // Success with alternative format
                }
                else
                {
                    return BadRequest(new { 
                        error = "Invalid ProductId format.", 
                        receivedValue = requestDto.ProductId,
                        length = requestDto.ProductId?.Length,
                        trimmedValue = productIdString,
                        trimmedLength = productIdString?.Length,
                        cleanedValue = productIdString
                    });
                }
            }

            // Create the proper DTO
            var quickOrderDto = new QuickOrderDto
            {
                ProductId = productId,
                Quantity = requestDto.Quantity,
                PhoneNumber = requestDto.PhoneNumber,
                CustomerName = requestDto.CustomerName,
                CustomerPhone = requestDto.CustomerPhone
            };

            // Debug: Log the received data
            Console.WriteLine($"Received ProductId: {quickOrderDto.ProductId}");
            Console.WriteLine($"Received Quantity: {quickOrderDto.Quantity}");
            Console.WriteLine($"Received PhoneNumber: {quickOrderDto.PhoneNumber}");

            Guid? userId = null;
            
            // Try to get user ID if authenticated, but don't require it
            try
            {
                userId = GetCurrentUserId();
            }
            catch (UnauthorizedAccessException)
            {
                // User is not authenticated, continue without user ID
            }
            
            // Auto-fill customer info from user claims if authenticated and not provided
            if (userId.HasValue && (string.IsNullOrEmpty(quickOrderDto.CustomerName) || string.IsNullOrEmpty(quickOrderDto.CustomerPhone)))
            {
                var userClaims = User.Claims;
                quickOrderDto.CustomerName = $"{userClaims.FirstOrDefault(c => c.Type == "FirstName")?.Value} {userClaims.FirstOrDefault(c => c.Type == "LastName")?.Value}".Trim();
                quickOrderDto.CustomerPhone = userClaims.FirstOrDefault(c => c.Type == ClaimTypes.MobilePhone)?.Value ?? "";
            }

            var whatsAppLink = await _cartService.GenerateQuickWhatsAppOrderAsync(userId, quickOrderDto, cancellationToken);
            return Ok(whatsAppLink);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = "Invalid operation.", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid request.", message = ex.Message });
        }
    }

    /// <summary>
    /// Get the count of items in user's cart
    /// </summary>
    [HttpGet("count")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<int>> GetCartCount(CancellationToken cancellationToken)
    {
        Guid? userId = null;
        try { if (User.Identity?.IsAuthenticated == true) userId = GetCurrentUserId(); } catch { }
        
        var count = await _cartService.GetCartCountAsync(userId, cancellationToken);
        return Ok(count);
    }

    /// <summary>
    /// Apply promo code to cart
    /// </summary>
    [HttpPost("apply-promo")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CartDto>> ApplyPromoCode([FromBody] ApplyPromoCodeDto applyPromoCodeDto, CancellationToken cancellationToken)
    {
        try
        {
            Guid? userId = null;
            try { if (User.Identity?.IsAuthenticated == true) userId = GetCurrentUserId(); } catch { }
            
            var cart = await _cartService.ApplyPromoCodeAsync(userId, applyPromoCodeDto.PromoCode, cancellationToken);
            return Ok(cart);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Remove applied promo code from cart
    /// </summary>
    [HttpDelete("promo")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CartDto>> RemovePromoCode(CancellationToken cancellationToken)
    {
        try
        {
            Guid? userId = null;
            try { if (User.Identity?.IsAuthenticated == true) userId = GetCurrentUserId(); } catch { }
            
            var cart = await _cartService.RemovePromoCodeAsync(userId, cancellationToken);
            return Ok(cart);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }
        return userId;
    }
}
