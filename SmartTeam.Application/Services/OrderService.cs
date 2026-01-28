using AutoMapper;
using Microsoft.Extensions.Options;
using SmartTeam.Application.Configuration;
using AutoMapper;
using Microsoft.Extensions.Options;
using SmartTeam.Application.Configuration;
using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;
using SmartTeam.Application.Helpers;

namespace SmartTeam.Application.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IWalletService _walletService;
    private readonly BonusSettings _bonusSettings;
    private readonly ICartService _cartService;

    public OrderService(IUnitOfWork unitOfWork, IMapper mapper, IWalletService walletService, IOptions<BonusSettings> bonusSettings, ICartService cartService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _walletService = walletService;
        _bonusSettings = bonusSettings.Value;
        _cartService = cartService;
    }

    public async Task<OrderDto> CreateOrderFromCartAsync(Guid userId, CreateOrderDto createOrderDto, CancellationToken cancellationToken = default)
    {
        // 1. Get User's Cart
        var cart = await _unitOfWork.Repository<Cart>()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart == null)
        {
            throw new InvalidOperationException("Cart not found.");
        }

        var cartItems = await _unitOfWork.Repository<CartItem>()
            .FindAsync(ci => ci.CartId == cart.Id, cancellationToken);

        if (!cartItems.Any())
        {
            throw new InvalidOperationException("Cart is empty.");
        }

        // 2. Validate Cart Items & Stock (Optional: Re-validate stock before order)
        decimal subTotal = 0;
        var orderItems = new List<OrderItem>();
        
        foreach (var cartItem in cartItems)
        {
            var product = await _unitOfWork.Repository<Product>().GetByIdAsync(cartItem.ProductId, cancellationToken);
            if (product == null) continue; // Skip if product deleted? Or throw?

            // Stock check logic here if strict
            
            // Create Order Item snapshot
            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = cartItem.ProductId,
                ProductName = product.Name,
                ProductSku = product.Sku,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.UnitPrice,
                TotalPrice = cartItem.TotalPrice
            };
            
            orderItems.Add(orderItem);
            subTotal += orderItem.TotalPrice;
        }

        // 3. Calculate Totals w/ Promo Code
        decimal promoDiscount = 0;
        if (cart.AppliedPromoCodeId.HasValue && cart.PromoCodeDiscountPercentage.HasValue)
        {
             promoDiscount = subTotal * (cart.PromoCodeDiscountPercentage.Value / 100);
             // Increment promo usage logic could be here or separately
             // Let's assume promo usage increment happens elsewhere or we add it here:
             var promo = await _unitOfWork.Repository<PromoCode>().GetByIdAsync(cart.AppliedPromoCodeId.Value, cancellationToken);
             if (promo != null)
             {
                 promo.CurrentUsageCount++;
                 _unitOfWork.Repository<PromoCode>().Update(promo);
                 
                 var promoUsage = new PromoCodeUsage
                 {
                     Id = Guid.NewGuid(),
                     PromoCodeId = promo.Id,
                     UserId = userId,
                     UsedAt = DateTime.UtcNow
                     // OrderId linkage would be good here if PromoCodeUsage has OrderId
                 };
                 await _unitOfWork.Repository<PromoCodeUsage>().AddAsync(promoUsage, cancellationToken);
             }
        }
        
        decimal totalAmount = subTotal - promoDiscount;

        // 3.5. Apply Wallet Balance if requested
        decimal walletDiscount = 0;
        if (createOrderDto.UseWalletAmount.HasValue && createOrderDto.UseWalletAmount.Value > 0)
        {
            var requestedWalletAmount = createOrderDto.UseWalletAmount.Value;
            
            // Validate wallet amount doesn't exceed order total
            if (requestedWalletAmount > totalAmount)
            {
                throw new InvalidOperationException($"Wallet amount ({requestedWalletAmount}) cannot exceed order total ({totalAmount}).");
            }
            
            // Debit wallet (this will validate sufficient balance)
            await _walletService.DebitWalletAsync(
                userId, 
                requestedWalletAmount, 
                "Used for order payment", 
                null, // OrderId will be set after order is created
                cancellationToken
            );
            
            walletDiscount = requestedWalletAmount;
            totalAmount -= walletDiscount;
        }

        // 4. Create Order
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = GenerateOrderNumber(),
            UserId = userId,
            CustomerName = createOrderDto.CustomerName,
            CustomerPhone = createOrderDto.CustomerPhone,
            DeliveryAddress = createOrderDto.DeliveryAddress,
            Latitude = createOrderDto.Latitude,
            Longitude = createOrderDto.Longitude,
            DeliveryNotes = createOrderDto.DeliveryNotes,
            SubTotal = subTotal,
            PromoCodeDiscount = promoDiscount,
            WalletDiscount = walletDiscount > 0 ? walletDiscount : null,
            TotalAmount = totalAmount, // After all discounts
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            BonusAwarded = false,
            BonusAmount = null // Calculated but not awarded yet
        };

        await _unitOfWork.Repository<Order>().AddAsync(order, cancellationToken);

        // 5. Save Order Items
        foreach (var item in orderItems)
        {
            item.OrderId = order.Id;
            await _unitOfWork.Repository<OrderItem>().AddAsync(item, cancellationToken);
        }

        // 6. Clear Cart
        await _cartService.ClearCartAsync(userId, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapOrderToDto(order, cancellationToken);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Repository<Order>().GetByIdAsync(orderId, cancellationToken);
        if (order == null) return null;
        
        return await MapOrderToDto(order, cancellationToken);
    }

    public async Task<IEnumerable<OrderListDto>> GetUserOrdersAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var orders = await _unitOfWork.Repository<Order>()
            .FindAsync(o => o.UserId == userId, cancellationToken);
            
        return orders.OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderListDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.CustomerName,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                StatusText = o.Status.ToString(),
                CreatedAt = o.CreatedAt,
                ItemsCount = 0 // Optimization: we'd need to include items count in query or fetch
            });
    }

    public async Task<PagedResultDto<OrderListDto>> GetAllOrdersAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var orders = await _unitOfWork.Repository<Order>()
            .FindAsync(o => true, cancellationToken);

        var ordersWithUsers = new List<(Order order, User? user)>();
        foreach (var order in orders)
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(order.UserId, cancellationToken);
            ordersWithUsers.Add((order, user));
        }

        var orderedList = ordersWithUsers.OrderByDescending(x => x.order.CreatedAt).ToList();
        var totalCount = orderedList.Count;
        var pagedItems = orderedList.Skip((page - 1) * pageSize).Take(pageSize);

        var orderDtos = pagedItems.Select(x => new OrderListDto
        {
            Id = x.order.Id,
            OrderNumber = x.order.OrderNumber,
            CustomerName = x.order.CustomerName,
            PharmacyName = x.user?.PharmacyName ?? string.Empty,
            TotalAmount = x.order.TotalAmount,
            Status = x.order.Status,
            StatusText = x.order.Status.ToString(),
            CreatedAt = x.order.CreatedAt,
            ItemsCount = 0 
        }).ToList();

        return new PagedResultDto<OrderListDto>
        {
            Items = orderDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResultDto<OrderListDto>> SearchOrdersByNameAsync(string customerName, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var orders = await _unitOfWork.Repository<Order>()
            .FindAsync(o => o.CustomerName.Contains(customerName), cancellationToken);

        var ordersWithUsers = new List<(Order order, User? user)>();
        foreach (var order in orders)
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(order.UserId, cancellationToken);
            ordersWithUsers.Add((order, user));
        }

        var orderedList = ordersWithUsers.OrderByDescending(x => x.order.CreatedAt).ToList();
        var totalCount = orderedList.Count;
        var pagedItems = orderedList.Skip((page - 1) * pageSize).Take(pageSize);

        var orderDtos = pagedItems.Select(x => new OrderListDto
        {
            Id = x.order.Id,
            OrderNumber = x.order.OrderNumber,
            CustomerName = x.order.CustomerName,
            PharmacyName = x.user?.PharmacyName ?? string.Empty,
            TotalAmount = x.order.TotalAmount,
            Status = x.order.Status,
            StatusText = x.order.Status.ToString(),
            CreatedAt = x.order.CreatedAt,
            ItemsCount = 0
        }).ToList();

        return new PagedResultDto<OrderListDto>
        {
            Items = orderDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<OrderDto> UpdateOrderStatusAsync(Guid orderId, UpdateOrderStatusDto updateDto, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Repository<Order>().GetByIdAsync(orderId, cancellationToken);
        if (order == null) throw new ArgumentException("Order not found");

        var oldStatus = order.Status;
        order.Status = updateDto.Status;
        order.UpdatedAt = TimeHelper.Now;

        if (updateDto.Status == OrderStatus.Confirmed && oldStatus != OrderStatus.Confirmed)
        {
            order.ConfirmedAt = TimeHelper.Now;
        }
        else if (updateDto.Status == OrderStatus.Delivered && oldStatus != OrderStatus.Delivered)
        {
            order.DeliveredAt = TimeHelper.Now;
            
            // Award Bonus
            if (!order.BonusAwarded)
            {
                await AwardBonusAsync(order, cancellationToken);
            }
        }
        else if (updateDto.Status == OrderStatus.Cancelled)
        {
            order.CancelledAt = TimeHelper.Now;
        }

        _unitOfWork.Repository<Order>().Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapOrderToDto(order, cancellationToken);
    }

    private async Task AwardBonusAsync(Order order, CancellationToken cancellationToken)
    {
        // Get Settings
        var bonusPercentage = _bonusSettings.BonusPercentage;
        var minOrderAmount = _bonusSettings.MinimumOrderForBonus;

        if (order.TotalAmount >= minOrderAmount)
        {
            var bonusAmount = order.TotalAmount * (bonusPercentage / 100);
            
            // Round to 2 decimals
            bonusAmount = Math.Round(bonusAmount, 2);

            if (bonusAmount > 0)
            {
                await _walletService.CreditBonusAsync(
                    order.UserId, 
                    bonusAmount, 
                    $"Bonus for Order #{order.OrderNumber}", 
                    order.Id, 
                    cancellationToken
                );

                order.BonusAwarded = true;
                order.BonusAmount = bonusAmount;
                // Order update will be saved by caller
            }
        }
    }

    private string GenerateOrderNumber()
    {
        // Simple generation: ORD-YYYYMMDD-XXXX
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersForExportAsync(DateTime? fromDate, DateTime? toDate, OrderStatus? status, CancellationToken cancellationToken = default)
    {
        // Fetch with Includes for items using inline filtered predicate
        var orders = await _unitOfWork.Repository<Order>().FindWithIncludesAsync(
            o => (!fromDate.HasValue || o.CreatedAt >= fromDate.Value) && 
                 (!toDate.HasValue || o.CreatedAt <= toDate.Value) &&
                 (!status.HasValue || o.Status == status.Value),
            o => o.Items
        );

        var orderDtos = new List<OrderDto>();
        foreach(var order in orders.OrderByDescending(o => o.CreatedAt))
        {
             orderDtos.Add(await MapOrderToDto(order, cancellationToken));
        }

        return orderDtos;
    }

    public async Task<OrderDto?> GetOrderWithDetailsAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        // MapOrderToDto fetches items inside. So standard GetById is enough if we use MapOrderToDto.
        // But better to use GetByIdWithIncludesAsync for optimization?
        // Current MapOrderToDto fetches items manually: await _unitOfWork.Repository<OrderItem>().FindAsync(...)
        // So fetching with includes here won't help MapOrderToDto unless we refactor MapOrderToDto.
        // Given current MapOrderToDto logic, let's just reuse GetOrderByIdAsync logic but ensuring it returns OrderDto.
        return await GetOrderByIdAsync(orderId, cancellationToken);
    }

    private async Task<OrderDto> MapOrderToDto(Order order, CancellationToken cancellationToken)
    {
        var items = await _unitOfWork.Repository<OrderItem>()
            .FindAsync(oi => oi.OrderId == order.Id, cancellationToken);

        // Fetch user to get PharmacyName
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(order.UserId, cancellationToken);
            
        var itemDtos = new List<OrderItemDto>();
        foreach(var item in items)
        {
            // Fetch product to get image
            var product = await _unitOfWork.Repository<Product>().GetByIdAsync(item.ProductId, cancellationToken);
            
            string? imageUrl = null;
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

                // Use thumbnail if available, otherwise use main image
                imageUrl = primaryImage?.ThumbnailUrl ?? primaryImage?.ImageUrl ?? product.ImageUrl;
            }

             itemDtos.Add(new OrderItemDto
             {
                 ProductId = item.ProductId,
                 ProductName = item.ProductName,
                 ProductSku = item.ProductSku,
                 Quantity = item.Quantity,
                 UnitPrice = item.UnitPrice,
                 TotalPrice = item.TotalPrice,
                 ProductImageUrl = imageUrl
             });
        }

        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            CustomerName = order.CustomerName,
            CustomerPhone = order.CustomerPhone,
            PharmacyName = user?.PharmacyName ?? string.Empty,
            DeliveryAddress = order.DeliveryAddress,
            Latitude = order.Latitude,
            Longitude = order.Longitude,
            DeliveryNotes = order.DeliveryNotes,
            Items = itemDtos,
            SubTotal = order.SubTotal,
            PromoCodeDiscount = order.PromoCodeDiscount,
            WalletDiscount = order.WalletDiscount,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            StatusText = order.Status.ToString(),
            CreatedAt = order.CreatedAt,
            ConfirmedAt = order.ConfirmedAt,
            DeliveredAt = order.DeliveredAt,
            BonusAwarded = order.BonusAwarded,
            BonusAmount = order.BonusAmount
        };
    }
}
