using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Configurations;

public class FilterConfiguration : IEntityTypeConfiguration<Filter>
{
    public void Configure(EntityTypeBuilder<Filter> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.Slug)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(f => f.Slug)
            .IsUnique();

        builder.Property(f => f.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(f => f.IsActive)
            .HasDefaultValue(true);

        builder.Property(f => f.SortOrder)
            .HasDefaultValue(0);

        builder.Property(f => f.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasMany(f => f.Options)
            .WithOne(fo => fo.Filter)
            .HasForeignKey(fo => fo.FilterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.ProductAttributeValues)
            .WithOne(av => av.Filter)
            .HasForeignKey(av => av.FilterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class FilterOptionConfiguration : IEntityTypeConfiguration<FilterOption>
{
    public void Configure(EntityTypeBuilder<FilterOption> builder)
    {
        builder.HasKey(fo => fo.Id);

        builder.Property(fo => fo.Value)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(fo => fo.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(fo => fo.Color)
            .HasMaxLength(50);

        builder.Property(fo => fo.IconUrl)
            .HasMaxLength(500);

        builder.Property(fo => fo.IsActive)
            .HasDefaultValue(true);

        builder.Property(fo => fo.SortOrder)
            .HasDefaultValue(0);

        builder.Property(fo => fo.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(fo => fo.Filter)
            .WithMany(f => f.Options)
            .HasForeignKey(fo => fo.FilterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(fo => fo.ProductAttributeValues)
            .WithOne(av => av.FilterOption)
            .HasForeignKey(av => av.FilterOptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(fo => new { fo.FilterId, fo.Value })
            .IsUnique();
    }
}

public class ProductAttributeValueConfiguration : IEntityTypeConfiguration<ProductAttributeValue>
{
    public void Configure(EntityTypeBuilder<ProductAttributeValue> builder)
    {
        builder.HasKey(av => av.Id);

        builder.Property(av => av.CustomValue)
            .HasMaxLength(500);

        builder.Property(av => av.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Relationships
        builder.HasOne(av => av.Product)
            .WithMany(p => p.AttributeValues)
            .HasForeignKey(av => av.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(av => av.Filter)
            .WithMany(f => f.ProductAttributeValues)
            .HasForeignKey(av => av.FilterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(av => av.FilterOption)
            .WithMany(fo => fo.ProductAttributeValues)
            .HasForeignKey(av => av.FilterOptionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: one attribute value per product per filter
        builder.HasIndex(av => new { av.ProductId, av.FilterId })
            .IsUnique();
    }
}
