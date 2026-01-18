using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Configurations;

public class ProductPdfConfiguration : IEntityTypeConfiguration<ProductPdf>
{
    public void Configure(EntityTypeBuilder<ProductPdf> builder)
    {
        builder.ToTable("ProductPdfs");

        builder.HasKey(pp => pp.Id);

        builder.Property(pp => pp.Id)
            .ValueGeneratedOnAdd();

        builder.Property(pp => pp.ProductId)
            .IsRequired();

        builder.Property(pp => pp.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(pp => pp.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(pp => pp.FilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(pp => pp.ContentType)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("application/pdf");

        builder.Property(pp => pp.FileSize)
            .IsRequired();

        builder.Property(pp => pp.Description)
            .HasMaxLength(1000);

        builder.Property(pp => pp.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(pp => pp.DownloadCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(pp => pp.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(pp => pp.UpdatedAt);

        builder.Property(pp => pp.CreatedBy)
            .IsRequired();

        builder.Property(pp => pp.UpdatedBy);

        // Relationships
        builder.HasOne(pp => pp.Product)
            .WithMany()
            .HasForeignKey(pp => pp.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pp => pp.CreatedByUser)
            .WithMany()
            .HasForeignKey(pp => pp.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pp => pp.UpdatedByUser)
            .WithMany()
            .HasForeignKey(pp => pp.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(pp => pp.ProductId);
        builder.HasIndex(pp => pp.FileName);
        builder.HasIndex(pp => pp.IsActive);
        builder.HasIndex(pp => pp.CreatedAt);
        builder.HasIndex(pp => pp.DownloadCount);

        // Unique constraint: One PDF per product
        builder.HasIndex(pp => pp.ProductId)
            .IsUnique()
            .HasDatabaseName("IX_ProductPdfs_ProductId_Unique");
    }
}
