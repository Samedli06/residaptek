using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Configurations;

public class ProductSpecificationConfiguration : IEntityTypeConfiguration<ProductSpecification>
{
    public void Configure(EntityTypeBuilder<ProductSpecification> builder)
    {
        builder.HasKey(ps => ps.Id);

        builder.Property(ps => ps.GroupName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ps => ps.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ps => ps.Value)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ps => ps.Unit)
            .HasMaxLength(50);

        builder.Property(ps => ps.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(ps => ps.SortOrder)
            .HasDefaultValue(0);

        builder.Property(ps => ps.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Relationships
        builder.HasOne(ps => ps.Product)
            .WithMany(p => p.Specifications)
            .HasForeignKey(ps => ps.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(ps => new { ps.ProductId, ps.GroupName });
        builder.HasIndex(ps => new { ps.ProductId, ps.SortOrder });
    }
}
