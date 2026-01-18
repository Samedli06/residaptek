using Microsoft.EntityFrameworkCore;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Data;

public class SmartTeamDbContext : DbContext
{
    public SmartTeamDbContext(DbContextOptions<SmartTeamDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Filter> Filters { get; set; }
    public DbSet<FilterOption> FilterOptions { get; set; }
    public DbSet<ProductAttributeValue> ProductAttributeValues { get; set; }
    public DbSet<Banner> Banners { get; set; }
    public DbSet<UserFavorite> UserFavorites { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<ProductSpecification> ProductSpecifications { get; set; }
    public DbSet<DownloadableFile> DownloadableFiles { get; set; }
    public DbSet<ProductPdf> ProductPdfs { get; set; }
    public DbSet<PromoCode> PromoCodes { get; set; }
    public DbSet<PromoCodeUsage> PromoCodeUsages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the Configurations folder
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SmartTeamDbContext).Assembly);

        // Set default values for audit fields
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var createdAtProperty = entityType.FindProperty("CreatedAt");
            if (createdAtProperty != null && createdAtProperty.ClrType == typeof(DateTime))
            {
                createdAtProperty.SetDefaultValueSql("GETUTCDATE()");
            }
        }
    }
}
