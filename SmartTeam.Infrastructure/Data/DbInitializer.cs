using SmartTeam.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SmartTeam.Application.Helpers;

namespace SmartTeam.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(SmartTeamDbContext context)
    {
        // Database creation and migration is now handled by Program.cs
        
        // Always ensure admin user exists with correct credentials
        await EnsureAdminUserAsync(context);



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
            Email = "admin@residaptek.az",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = TimeHelper.Now
        };
        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin123!");
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User",
            Email = "test@residaptek.az",
            Role = UserRole.NormalUser,
            IsActive = true,
            CreatedAt = TimeHelper.Now
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
                CreatedAt = TimeHelper.Now
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
                CreatedAt = TimeHelper.Now
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
                CreatedAt = TimeHelper.Now
            };
            await context.Filters.AddAsync(priceFilter);
        }

        // Seed Sample Products


        await context.SaveChangesAsync();


    }



    private static async Task EnsureAdminUserAsync(SmartTeamDbContext context)
    {
        var adminEmail = "admin@residaptek.az";
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
                CreatedAt = TimeHelper.Now
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
            adminUser.UpdatedAt = TimeHelper.Now;
            context.Users.Update(adminUser);
            await context.SaveChangesAsync();
        }

    }


}
