using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Configurations;

public class UserFavoriteConfiguration : IEntityTypeConfiguration<UserFavorite>
{
    public void Configure(EntityTypeBuilder<UserFavorite> builder)
    {
        builder.HasKey(uf => uf.Id);

        builder.Property(uf => uf.Id)
            .ValueGeneratedOnAdd();

        builder.Property(uf => uf.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Configure relationships
        builder.HasOne(uf => uf.User)
            .WithMany(u => u.Favorites)
            .HasForeignKey(uf => uf.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(uf => uf.Product)
            .WithMany(p => p.Favorites)
            .HasForeignKey(uf => uf.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Create unique constraint to prevent duplicate favorites
        builder.HasIndex(uf => new { uf.UserId, uf.ProductId })
            .IsUnique()
            .HasDatabaseName("IX_UserFavorites_UserId_ProductId");

        // Table name
        builder.ToTable("UserFavorites");
    }
}
