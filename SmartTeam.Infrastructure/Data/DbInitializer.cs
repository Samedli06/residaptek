using SmartTeam.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace SmartTeam.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(SmartTeamDbContext context)
    {
        // Database creation and migration is now handled by Program.cs
        
        // Always ensure admin user exists with correct credentials
        await EnsureAdminUserAsync(context);

        // Update categories hierarchy unconditionally (ensures new structure exists)
        await UpdateCategoriesAsync(context);

        // Only seed initial data if no users exist (excluding the admin we just created/updated)
        var userCount = await context.Users.CountAsync();
        if (userCount > 1)
        {
            return; // Database has been seeded with more than just admin
        }
        var passwordHasher = new PasswordHasher<User>();
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@gunaybeauty.az",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin123!");
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User",
            Email = "test@gunaybeauty.az",
            Role = UserRole.NormalUser,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        testUser.PasswordHash = passwordHasher.HashPassword(testUser, "Test123!");

        await context.Users.AddRangeAsync(adminUser, testUser);
        
        // Seed Filters
        var brandFilter = await context.Filters.FirstOrDefaultAsync(f => f.Slug == "brand");
        if (brandFilter == null)
        {
            brandFilter = new Filter
            {
                Id = Guid.NewGuid(),
                Name = "Brand",
                Slug = "brand",
                Type = FilterType.Select,
                IsActive = true,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow
            };
            await context.Filters.AddAsync(brandFilter);
        }

        var sizeFilter = await context.Filters.FirstOrDefaultAsync(f => f.Slug == "size");
        if (sizeFilter == null)
        {
            sizeFilter = new Filter
            {
                Id = Guid.NewGuid(),
                Name = "Size",
                Slug = "size",
                Type = FilterType.Select,
                IsActive = true,
                SortOrder = 2,
                CreatedAt = DateTime.UtcNow
            };
            await context.Filters.AddAsync(sizeFilter);
        }

        var priceFilter = await context.Filters.FirstOrDefaultAsync(f => f.Slug == "price-range");
        if (priceFilter == null)
        {
            priceFilter = new Filter
            {
                Id = Guid.NewGuid(),
                Name = "Price Range",
                Slug = "price-range",
                Type = FilterType.Range,
                IsActive = true,
                SortOrder = 3,
                CreatedAt = DateTime.UtcNow
            };
            await context.Filters.AddAsync(priceFilter);
        }

        // Seed Sample Products
        var electronics = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Deodorants");
        var clothing = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Lipstick");

        if (electronics != null && clothing != null)
        {
             var product1 = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Nivea Men Deodorant",
                Slug = "nivea-men-deodorant",
                Description = "24h protection",
                ShortDescription = "Deodorant for men",
                Sku = "NIVMEN001",
                CategoryId = electronics.Id,
                IsActive = true,
                IsHotDeal = true,
                StockQuantity = 50,
                Price = 12m,
                DiscountedPrice = 10m,
                CreatedAt = DateTime.UtcNow
            };

            var product2 = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Mac Lipstick Red",
                Slug = "mac-lipstick-red",
                Description = "Classic red lipstick",
                ShortDescription = "Red Lipstick",
                Sku = "MACRED001",
                CategoryId = clothing.Id,
                IsActive = true,
                IsHotDeal = false,
                StockQuantity = 100,
                Price = 45m,
                CreatedAt = DateTime.UtcNow
            };
            
            if (!await context.Products.AnyAsync()) 
            {
                 await context.Products.AddRangeAsync(product1, product2);
            }
        }

        await context.SaveChangesAsync();

        // Seed Filter Options if not exist
        if (!await context.FilterOptions.AnyAsync(fo => fo.FilterId == brandFilter.Id))
        {
            var brandOptions = new[]
            {
                new FilterOption { Id = Guid.NewGuid(), FilterId = brandFilter.Id, Value = "apple", DisplayName = "Apple", IsActive = true, SortOrder = 1, CreatedAt = DateTime.UtcNow },
                new FilterOption { Id = Guid.NewGuid(), FilterId = brandFilter.Id, Value = "samsung", DisplayName = "Samsung", IsActive = true, SortOrder = 2, CreatedAt = DateTime.UtcNow },
                new FilterOption { Id = Guid.NewGuid(), FilterId = brandFilter.Id, Value = "nike", DisplayName = "Nike", IsActive = true, SortOrder = 3, CreatedAt = DateTime.UtcNow },
                new FilterOption { Id = Guid.NewGuid(), FilterId = brandFilter.Id, Value = "charlotte-tilbury", DisplayName = "Charlotte Tilbury", IsActive = true, SortOrder = 4, CreatedAt = DateTime.UtcNow },
                new FilterOption { Id = Guid.NewGuid(), FilterId = brandFilter.Id, Value = "mac", DisplayName = "MAC", IsActive = true, SortOrder = 5, CreatedAt = DateTime.UtcNow }
            };
            await context.FilterOptions.AddRangeAsync(brandOptions);
        }

        if (!await context.FilterOptions.AnyAsync(fo => fo.FilterId == sizeFilter.Id))
        {
            var sizeOptions = new[]
            {
                new FilterOption { Id = Guid.NewGuid(), FilterId = sizeFilter.Id, Value = "s", DisplayName = "Small", IsActive = true, SortOrder = 1, CreatedAt = DateTime.UtcNow },
                new FilterOption { Id = Guid.NewGuid(), FilterId = sizeFilter.Id, Value = "m", DisplayName = "Medium", IsActive = true, SortOrder = 2, CreatedAt = DateTime.UtcNow },
                new FilterOption { Id = Guid.NewGuid(), FilterId = sizeFilter.Id, Value = "l", DisplayName = "Large", IsActive = true, SortOrder = 3, CreatedAt = DateTime.UtcNow }
            };
            await context.FilterOptions.AddRangeAsync(sizeOptions);
        }

        await context.SaveChangesAsync();
    }

    private static async Task UpdateCategoriesAsync(SmartTeamDbContext context)
    {
        // 1. Skin Care
        var skinCare = await EnsureCategoryAsync(context, "Skin Care", null, 1);
        await EnsureCategoryAsync(context, "Acne Treatments & Kits", skinCare.Id, 1);
        await EnsureCategoryAsync(context, "Anti-Aging Skin Care", skinCare.Id, 2);
        await EnsureCategoryAsync(context, "Compressed Skin Care Mask Sheets", skinCare.Id, 3);
        await EnsureCategoryAsync(context, "Eye Creams", skinCare.Id, 4);
        await EnsureCategoryAsync(context, "Face Moisturizers", skinCare.Id, 5);
        await EnsureCategoryAsync(context, "Face Serums", skinCare.Id, 6);
        await EnsureCategoryAsync(context, "Facial Cleansers", skinCare.Id, 7);
        await EnsureCategoryAsync(context, "Lip Balms & Treatments", skinCare.Id, 8);
        await EnsureCategoryAsync(context, "Skin Care Masks & Peels", skinCare.Id, 9);
        await EnsureCategoryAsync(context, "Lotions & Moisturizers", skinCare.Id, 10);
        await EnsureCategoryAsync(context, "Sunscreen", skinCare.Id, 11);
        await EnsureCategoryAsync(context, "Toners & Astringents", skinCare.Id, 12);
        
        var deodorantsParent = await EnsureCategoryAsync(context, "Deodorants & Anti-Perspirant", skinCare.Id, 13);
        await EnsureCategoryAsync(context, "Deodorants", deodorantsParent.Id, 1);

        // 2. Cosmetics
        var cosmetics = await EnsureCategoryAsync(context, "Cosmetics", null, 2);
        
        // Face Makeup
        var faceMakeup = await EnsureCategoryAsync(context, "Face Makeup", cosmetics.Id, 1);
        await EnsureCategoryAsync(context, "Foundation", faceMakeup.Id, 1);
        await EnsureCategoryAsync(context, "Concealer", faceMakeup.Id, 2);
        await EnsureCategoryAsync(context, "Primers", faceMakeup.Id, 3);
        await EnsureCategoryAsync(context, "Setting Powder", faceMakeup.Id, 4);
        await EnsureCategoryAsync(context, "BB & CC Creams", faceMakeup.Id, 5);
        await EnsureCategoryAsync(context, "Setting Sprays", faceMakeup.Id, 6);

        // Eye Makeup
        var eyeMakeup = await EnsureCategoryAsync(context, "Eye Makeup", cosmetics.Id, 2);
        await EnsureCategoryAsync(context, "Eyeshadow", eyeMakeup.Id, 1);
        await EnsureCategoryAsync(context, "Mascara", eyeMakeup.Id, 2);
        await EnsureCategoryAsync(context, "Eyeliner", eyeMakeup.Id, 3);
        await EnsureCategoryAsync(context, "Eyebrows", eyeMakeup.Id, 4);
        await EnsureCategoryAsync(context, "Eye Primers", eyeMakeup.Id, 5);

        // Lip Makeup
        var lipMakeup = await EnsureCategoryAsync(context, "Lip Makeup", cosmetics.Id, 3);
        await EnsureCategoryAsync(context, "Lipstick", lipMakeup.Id, 1);
        await EnsureCategoryAsync(context, "Lip Gloss & Oils", lipMakeup.Id, 2);
        await EnsureCategoryAsync(context, "Lip Liner", lipMakeup.Id, 3);
        await EnsureCategoryAsync(context, "Liquid Lipstick", lipMakeup.Id, 4);
        await EnsureCategoryAsync(context, "Lip Care", lipMakeup.Id, 5);

        // Cheek & Glow
        var cheekGlow = await EnsureCategoryAsync(context, "Cheek & Glow", cosmetics.Id, 4);
        await EnsureCategoryAsync(context, "Blush", cheekGlow.Id, 1);
        await EnsureCategoryAsync(context, "Bronzer", cheekGlow.Id, 2);
        await EnsureCategoryAsync(context, "Highlighter", cheekGlow.Id, 3);
        await EnsureCategoryAsync(context, "Contour", cheekGlow.Id, 4);

        // Tools & Brushes
        var toolsBrushes = await EnsureCategoryAsync(context, "Tools & Brushes", cosmetics.Id, 5);
        await EnsureCategoryAsync(context, "Makeup Brushes", toolsBrushes.Id, 1);
        await EnsureCategoryAsync(context, "Sponges & Blenders", toolsBrushes.Id, 2);
        await EnsureCategoryAsync(context, "Eyelash Curlers", toolsBrushes.Id, 3);
        await EnsureCategoryAsync(context, "Brush Cleaners", toolsBrushes.Id, 4);
        await EnsureCategoryAsync(context, "Makeup Bags", toolsBrushes.Id, 5);

        // 3. Gift Sets
        await EnsureCategoryAsync(context, "Gift Sets", null, 3);

        // 4. Perfumes
        await EnsureCategoryAsync(context, "Perfumes", null, 4);

        await context.SaveChangesAsync();
    }

    private static async Task<Category> EnsureCategoryAsync(SmartTeamDbContext context, string name, Guid? parentId, int sortOrder)
    {
        var slug = GenerateSlug(name);
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Slug == slug && c.ParentCategoryId == parentId);

        if (category == null)
        {
            category = new Category
            {
                Id = Guid.NewGuid(),
                Name = name,
                Slug = slug,
                ParentCategoryId = parentId,
                IsActive = true,
                SortOrder = sortOrder,
                CreatedAt = DateTime.UtcNow
            };
            await context.Categories.AddAsync(category);
            // Save immediately to generate ID for children
            await context.SaveChangesAsync(); 
        }
        else
        {
            // Update existing if check passes (to correct sort order or parent if name matches)
            // But here we mainly ensure existence.
            if (category.SortOrder != sortOrder)
            {
                category.SortOrder = sortOrder;
                context.Categories.Update(category);
                await context.SaveChangesAsync();
            }
        }
        
        return category;
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" & ", "-and-")
            .Replace("&", "and")
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace(",", "")
            .Replace("--", "-");
    }

    private static async Task EnsureAdminUserAsync(SmartTeamDbContext context)
    {
        var adminEmail = "admin@gunaybeauty.az";
        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        var passwordHasher = new PasswordHasher<User>();

        if (adminUser == null)
        {
            // Create new admin user
            adminUser = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Admin",
                LastName = "User",
                Email = adminEmail,
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin123!");
            await context.Users.AddAsync(adminUser);
            await context.SaveChangesAsync();
        }
        else
        {
            // Update existing admin user's password and ensure correct role
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin123!");
            adminUser.Role = UserRole.Admin;
            adminUser.IsActive = true;
            adminUser.UpdatedAt = DateTime.UtcNow;
            context.Users.Update(adminUser);
            await context.SaveChangesAsync();
        }
    }
}
