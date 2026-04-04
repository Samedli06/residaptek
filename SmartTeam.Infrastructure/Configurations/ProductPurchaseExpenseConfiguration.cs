using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Configurations;

public class ProductPurchaseExpenseConfiguration : IEntityTypeConfiguration<ProductPurchaseExpense>
{
    public void Configure(EntityTypeBuilder<ProductPurchaseExpense> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(e => e.InvoiceNumber)
            .IsUnique();

        builder.Property(e => e.ProductName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.UnitPurchasePrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.TotalExpense)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.SupplierName)
            .HasMaxLength(200);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        // FK to Product — restrict delete to preserve historical expense records
        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
