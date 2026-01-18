using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Configurations;

public class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(b => b.Description)
            .HasMaxLength(1000);

        builder.Property(b => b.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(b => b.MobileImageUrl)
            .HasMaxLength(500);

        builder.Property(b => b.LinkUrl)
            .HasMaxLength(500);

        builder.Property(b => b.ButtonText)
            .HasMaxLength(100);

        builder.Property(b => b.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(b => b.IsActive)
            .HasDefaultValue(true);

        builder.Property(b => b.SortOrder)
            .HasDefaultValue(0);

        builder.Property(b => b.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes
        builder.HasIndex(b => new { b.Type, b.IsActive });
        builder.HasIndex(b => new { b.StartDate, b.EndDate });
    }
}

