using AutoMapper;
using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;

namespace SmartTeam.Application.Services;

public class CartService : ICartService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IWhatsAppService _whatsAppService;

    public CartService(IUnitOfWork unitOfWork, IMapper mapper, IWhatsAppService whatsAppService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _whatsAppService = whatsAppService;
    }

    public async Task<CartDto> GetUserCartAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        if (!userId.HasValue)
        {
            // For anonymous users, get the most recent cart for the anonymous user
            var anonymousUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var cart = await _unitOfWork.Repository<Cart>()
                .FirstOrDefaultAsync(c => c.UserId == anonymousUserId, cancellationToken);
            
            if (cart == null)
            {
                // No cart exists for anonymous user, return empty cart
                return new CartDto
                {
                    Id = Guid.Empty,
                    UserId = Guid.Empty,
                    Items = new List<CartItemDto>(),
                    TotalAmount = 0,
                    TotalDiscount = 0,
                    TotalQuantity = 0,
                    CreatedAt = DateTime.UtcNow
                };
            }
            
            return await MapCartToDto(cart, cancellationToken);
        }
        
        var userCart = await GetOrCreateCartAsync(userId, cancellationToken);
        return await MapCartToDto(userCart, cancellationToken);
    }

    public async Task<CartDto> AddToCartAsync(Guid? userId, AddToCartDto addToCartDto, CancellationToken cancellationToken = default)
    {
        // Validate product exists and is active
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(addToCartDto.ProductId, cancellationToken);
        if (product == null || !product.IsActive)
        {
            throw new ArgumentException("Product not found or inactive.");
        }

        // Check stock availability
        if (product.StockQuantity < addToCartDto.Quantity)
        {
            throw new InvalidOperationException($"Insufficient stock. Available: {product.StockQuantity}");
        }

        // Use discounted price if available, otherwise regular price
        decimal unitPrice = product.DiscountedPrice ?? product.Price;

        if (unitPrice <= 0)
        {
             // Fallback or error if price is not set correctly (though database should enforce it)
             throw new InvalidOperationException("Product price is invalid.");
        }

        var cart = await GetOrCreateCartAsync(userId, cancellationToken);

        // Check if item already exists in cart
        var existingItem = await _unitOfWork.Repository<CartItem>()
            .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == addToCartDto.ProductId, cancellationToken);

        if (existingItem != null)
        {
            // Update quantity
            var newQuantity = existingItem.Quantity + addToCartDto.Quantity;
            
            // Check stock for new quantity
            if (product.StockQuantity < newQuantity)
            {
                throw new InvalidOperationException($"Insufficient stock. Available: {product.StockQuantity}, In cart: {existingItem.Quantity}");
            }

            existingItem.Quantity = newQuantity;
            existingItem.UnitPrice = unitPrice; // Update price in case it changed
            existingItem.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<CartItem>().Update(existingItem);
        }
        else
        {
            // Add new item
            var cartItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductId = addToCartDto.ProductId,
                Quantity = addToCartDto.Quantity,
                UnitPrice = unitPrice,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<CartItem>().AddAsync(cartItem, cancellationToken);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<Cart>().Update(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapCartToDto(cart, cancellationToken);
    }

    public async Task<CartDto> UpdateCartItemAsync(Guid? userId, Guid cartItemId, UpdateCartItemDto updateCartItemDto, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(userId, cancellationToken);
        
        var cartItem = await _unitOfWork.Repository<CartItem>()
            .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.CartId == cart.Id, cancellationToken);

        if (cartItem == null)
        {
            throw new ArgumentException("Cart item not found.");
        }

        if (updateCartItemDto.Quantity <= 0)
        {
            // Remove item if quantity is 0 or negative
            _unitOfWork.Repository<CartItem>().Remove(cartItem);
        }
        else
        {
            // Check stock availability
            var product = await _unitOfWork.Repository<Product>().GetByIdAsync(cartItem.ProductId, cancellationToken);
            if (product != null && product.StockQuantity < updateCartItemDto.Quantity)
            {
                throw new InvalidOperationException($"Insufficient stock. Available: {product.StockQuantity}");
            }

            cartItem.Quantity = updateCartItemDto.Quantity;
            cartItem.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<CartItem>().Update(cartItem);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<Cart>().Update(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapCartToDto(cart, cancellationToken);
    }

    public async Task<CartDto> RemoveFromCartAsync(Guid? userId, Guid cartItemId, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(userId, cancellationToken);
        
        var cartItem = await _unitOfWork.Repository<CartItem>()
            .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.CartId == cart.Id, cancellationToken);

        if (cartItem == null)
        {
            throw new ArgumentException("Cart item not found.");
        }

        _unitOfWork.Repository<CartItem>().Remove(cartItem);
        
        cart.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<Cart>().Update(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapCartToDto(cart, cancellationToken);
    }

    public async Task<bool> ClearCartAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(userId, cancellationToken);

        if (cart == null)
        {
            return true; // Cart doesn't exist, consider it cleared
        }

        var cartItems = await _unitOfWork.Repository<CartItem>()
            .FindAsync(ci => ci.CartId == cart.Id, cancellationToken);

        _unitOfWork.Repository<CartItem>().RemoveRange(cartItems);
        
        cart.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<Cart>().Update(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<WhatsAppLinkDto> GenerateWhatsAppOrderAsync(Guid? userId, WhatsAppOrderDto orderDto, CancellationToken cancellationToken = default)
    {
        CartDto cartDto;

        if (orderDto.CartId.HasValue && orderDto.CartId.Value != Guid.Empty)
        {
            var cart = await _unitOfWork.Repository<Cart>().GetByIdAsync(orderDto.CartId.Value, cancellationToken);
            if (cart == null)
            {
                throw new InvalidOperationException("Cart not found.");
            }
            cartDto = await MapCartToDto(cart, cancellationToken);
        }
        else if (userId.HasValue)
        {
            var cart = await GetOrCreateCartAsync(userId, cancellationToken);
            cartDto = await MapCartToDto(cart, cancellationToken);
        }
        else
        {
            if (orderDto.Items == null || !orderDto.Items.Any())
            {
                throw new InvalidOperationException("Cart is empty or items not provided.");
            }
            cartDto = new CartDto
            {
                Items = orderDto.Items,
                TotalAmount = orderDto.TotalAmount
            };
        }

        if (!cartDto.Items.Any())
        {
            throw new InvalidOperationException("Cart is empty.");
        }

        orderDto.Items = cartDto.Items;
        orderDto.TotalAmount = cartDto.TotalAmount;

        var message = _whatsAppService.FormatOrderMessage(orderDto);
        var whatsAppUrl = _whatsAppService.GenerateWhatsAppUrl(orderDto.PhoneNumber, message);

        return new WhatsAppLinkDto
        {
            WhatsAppUrl = whatsAppUrl,
            Message = message
        };
    }

    public async Task<WhatsAppLinkDto> GenerateQuickWhatsAppOrderAsync(Guid? userId, QuickOrderDto quickOrderDto, CancellationToken cancellationToken = default)
    {
        // Validate product exists and is active
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(quickOrderDto.ProductId, cancellationToken);
        if (product == null || !product.IsActive)
        {
            throw new ArgumentException("Product not found or inactive.");
        }

        // Validate quantity
        if (quickOrderDto.Quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.");
        }

        // Check stock availability
        if (product.StockQuantity < quickOrderDto.Quantity)
        {
            throw new InvalidOperationException($"Insufficient stock. Available: {product.StockQuantity}");
        }

        decimal unitPrice = product.DiscountedPrice ?? product.Price;
        decimal totalPrice = unitPrice * quickOrderDto.Quantity;

        // Create a single cart item for this product
        var cartItem = new CartItemDto
        {
            Id = Guid.NewGuid(),
            CartId = Guid.Empty, // Not associated with a cart
            ProductId = product.Id,
            ProductName = product.Name,
            ProductSku = product.Sku,
            ProductDescription = product.ShortDescription,
            ProductImageUrl = product.ImageUrl,
            Quantity = quickOrderDto.Quantity,
            UnitPrice = unitPrice,
            TotalPrice = totalPrice,
            CreatedAt = DateTime.UtcNow
        };

        // Create WhatsApp order DTO
        var whatsAppOrderDto = new WhatsAppOrderDto
        {
            PhoneNumber = quickOrderDto.PhoneNumber,
            CustomerName = quickOrderDto.CustomerName,
            CustomerPhone = quickOrderDto.CustomerPhone,
            Items = new List<CartItemDto> { cartItem },
            TotalAmount = totalPrice,
            Currency = "AZN"
        };

        // Generate WhatsApp message
        var message = _whatsAppService.FormatOrderMessage(whatsAppOrderDto);
        var whatsAppUrl = _whatsAppService.GenerateWhatsAppUrl(quickOrderDto.PhoneNumber, message);

        return new WhatsAppLinkDto
        {
            WhatsAppUrl = whatsAppUrl,
            Message = message
        };
    }

    private async Task<Cart> GetOrCreateCartAsync(Guid? userId, CancellationToken cancellationToken)
    {
        
        if (userId.HasValue)
        {
            // Authenticated user - use existing logic
            var cart = await _unitOfWork.Repository<Cart>()
                .FirstOrDefaultAsync(c => c.UserId == userId.Value, cancellationToken);

            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    UserId = userId.Value,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<Cart>().AddAsync(cart, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return cart;
        }
        else
        {
            // Anonymous user - use a special "Anonymous" user ID
            // First, check if the anonymous user exists, if not create it
            var anonymousUserId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Special GUID for anonymous user
            
            var anonymousUser = await _unitOfWork.Repository<User>().GetByIdAsync(anonymousUserId, cancellationToken);
            if (anonymousUser == null)
            {
                // Create the anonymous user if it doesn't exist
                anonymousUser = new User
                {
                    Id = anonymousUserId,
                    FirstName = "Anonymous",
                    LastName = "User",
                    Email = "anonymous@smartteam.az",
                    PasswordHash = "anonymous", // This user can't login
                    Role = UserRole.NormalUser,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                
                await _unitOfWork.Repository<User>().AddAsync(anonymousUser, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            
            // Try to get existing cart for anonymous user, if not create new one
            var existingCart = await _unitOfWork.Repository<Cart>()
                .FirstOrDefaultAsync(c => c.UserId == anonymousUserId, cancellationToken);
            
            if (existingCart != null)
            {
                return existingCart;
            }
            
            // Create a new cart for this anonymous session
            var cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = anonymousUserId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Cart>().AddAsync(cart, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return cart;
        }
    }

    private async Task<CartDto> MapCartToDto(Cart cart, CancellationToken cancellationToken)
    {
        var cartItems = await _unitOfWork.Repository<CartItem>()
            .FindAsync(ci => ci.CartId == cart.Id, cancellationToken);

        var cartItemDtos = new List<CartItemDto>();
        decimal totalDiscount = 0;
        decimal totalPriceBeforeDiscount = 0;

        foreach (var item in cartItems)
        {
            var product = await _unitOfWork.Repository<Product>().GetByIdAsync(item.ProductId, cancellationToken);
            if (product != null)
            {
                // Get primary image from ProductImage collection
                var primaryImage = await _unitOfWork.Repository<ProductImage>()
                    .FirstOrDefaultAsync(pi => pi.ProductId == product.Id && pi.IsPrimary, cancellationToken);

                // If no primary image found, get the first available image
                if (primaryImage == null)
                {
                    primaryImage = await _unitOfWork.Repository<ProductImage>()
                        .FirstOrDefaultAsync(pi => pi.ProductId == product.Id, cancellationToken);
                }

                // Determine the image URL to use (prioritize thumbnail, then image, then main product image)
                var imageUrl = primaryImage?.ThumbnailUrl ?? 
                              primaryImage?.ImageUrl ?? 
                              product.ImageUrl;

                decimal originalPrice = product.Price;
                decimal itemDiscount = 0;

                // Calculate discount per item: (original price - discounted price) * quantity
                if (product.DiscountedPrice.HasValue)
                {
                    var discountedPrice = product.DiscountedPrice.Value;
                    itemDiscount = (originalPrice - discountedPrice) * item.Quantity;
                }

                // Add to totals
                totalPriceBeforeDiscount += originalPrice * item.Quantity;
                totalDiscount += itemDiscount;

                // Calculate total price for this item line (using unit price which should match discount if applicable)
                // However, unit price in cart item is stored at time of add. Logic above updates it on re-add.
                // Discrepancy check: Should we use stored unit price or current product price?
                // Stored unit price reflects price at time of adding to cart.
                // But for display "Original Price" context, we use current product price.
                
                // Let's use stored unit price for the total calculation to be safe with what's in DB
                // But update "original price" context for UI to show savings.

                cartItemDtos.Add(new CartItemDto
                {
                    Id = item.Id,
                    CartId = item.CartId,
                    ProductId = item.ProductId,
                    ProductName = product.Name,
                    ProductSku = product.Sku,
                    ProductDescription = product.ShortDescription ?? product.Description,
                    ProductImageUrl = imageUrl,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice,
                    CreatedAt = item.CreatedAt
                });
            }
        }

        // Get applied promo code if exists
        string? appliedPromoCodeName = null;
        decimal? promoCodeDiscountPercentage = cart.PromoCodeDiscountPercentage;
        decimal promoCodeDiscountAmount = 0;

        if (cart.AppliedPromoCodeId.HasValue)
        {
            var appliedPromoCode = await _unitOfWork.Repository<PromoCode>()
                .GetByIdAsync(cart.AppliedPromoCodeId.Value, cancellationToken);
            
            if (appliedPromoCode != null)
            {
                appliedPromoCodeName = appliedPromoCode.Code;
                promoCodeDiscountPercentage = appliedPromoCode.DiscountPercentage;
                
                // Calculate promo code discount amount
                var subTotal = cartItemDtos.Sum(i => i.TotalPrice);
                promoCodeDiscountAmount = subTotal * (promoCodeDiscountPercentage.Value / 100);
            }
        }

        var subTotalAmount = cartItemDtos.Sum(i => i.TotalPrice);
        var finalAmount = subTotalAmount - promoCodeDiscountAmount;

        return new CartDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            Items = cartItemDtos,
            SubTotal = subTotalAmount,
            TotalAmount = subTotalAmount, // SubTotal is often same as TotalAmount before extra fees
            TotalPriceBeforeDiscount = totalPriceBeforeDiscount,
            TotalDiscount = totalDiscount,
            TotalQuantity = cartItemDtos.Sum(i => i.Quantity),
            AppliedPromoCode = appliedPromoCodeName,
            PromoCodeDiscountPercentage = promoCodeDiscountPercentage,
            PromoCodeDiscountAmount = promoCodeDiscountAmount,
            FinalAmount = finalAmount,
            CreatedAt = cart.CreatedAt,
            UpdatedAt = cart.UpdatedAt
        };
    }

    public async Task<int> GetCartCountAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(userId, cancellationToken);
        
        // Count only items with active products
        var cartItems = await _unitOfWork.Repository<CartItem>()
            .FindAsync(ci => ci.CartId == cart.Id, cancellationToken);

        var activeCount = 0;
        foreach (var item in cartItems)
        {
            var product = await _unitOfWork.Repository<Product>()
                .FirstOrDefaultAsync(p => p.Id == item.ProductId && p.IsActive, cancellationToken);
            
            if (product != null)
            {
                activeCount += item.Quantity;
            }
        }

        return activeCount;
    }

    public async Task<CartDto> ApplyPromoCodeAsync(Guid? userId, string promoCode, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(userId, cancellationToken);

        if (cart == null)
        {
            throw new InvalidOperationException("Cart not found.");
        }

        // Validate promo code
        var promoCodeEntity = await _unitOfWork.Repository<PromoCode>()
            .FirstOrDefaultAsync(p => p.Code.ToLower() == promoCode.ToLower(), cancellationToken);

        if (promoCodeEntity == null)
        {
            throw new ArgumentException("Invalid promo code.");
        }

        if (!promoCodeEntity.IsActive)
        {
            throw new InvalidOperationException("Promo code is not active.");
        }

        if (promoCodeEntity.ExpirationDate.HasValue && promoCodeEntity.ExpirationDate.Value < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Promo code has expired.");
        }

        if (promoCodeEntity.UsageLimit.HasValue && promoCodeEntity.CurrentUsageCount >= promoCodeEntity.UsageLimit.Value)
        {
            throw new InvalidOperationException("Promo code usage limit exceeded.");
        }

        // Apply promo code to cart
        cart.AppliedPromoCodeId = promoCodeEntity.Id;
        cart.PromoCodeDiscountPercentage = promoCodeEntity.DiscountPercentage;
        cart.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<Cart>().Update(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapCartToDto(cart, cancellationToken);
    }

    public async Task<CartDto> RemovePromoCodeAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(userId, cancellationToken);

        if (cart == null)
        {
            throw new InvalidOperationException("Cart not found.");
        }

        // Remove promo code from cart
        cart.AppliedPromoCodeId = null;
        cart.PromoCodeDiscountPercentage = null;
        cart.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<Cart>().Update(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapCartToDto(cart, cancellationToken);
    }
}
