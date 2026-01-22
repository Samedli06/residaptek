using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Configurations;

public class UserWalletConfiguration : IEntityTypeConfiguration<UserWallet>
{
    public void Configure(EntityTypeBuilder<UserWallet> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Balance)
            .HasColumnType("decimal(18,2)");

        builder.HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => w.UserId)
            .IsUnique();

        builder.HasMany(w => w.Transactions)
            .WithOne(wt => wt.Wallet)
            .HasForeignKey(wt => wt.WalletId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.HasKey(wt => wt.Id);

        builder.Property(wt => wt.Type)
            .HasConversion<int>();

        builder.Property(wt => wt.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Property(wt => wt.BalanceBefore)
            .HasColumnType("decimal(18,2)");

        builder.Property(wt => wt.BalanceAfter)
            .HasColumnType("decimal(18,2)");

        builder.Property(wt => wt.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(wt => wt.CreatedAt);
    }
}
