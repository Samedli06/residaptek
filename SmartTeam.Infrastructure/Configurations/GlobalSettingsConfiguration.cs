using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Configurations;

public class GlobalSettingsConfiguration : IEntityTypeConfiguration<GlobalSettings>
{
    public void Configure(EntityTypeBuilder<GlobalSettings> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.MinimumOrderAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(s => s.IsMinimumOrderAmountEnabled)
            .HasDefaultValue(false);

        builder.Property(s => s.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()");
            
        // Ensure only one record is used
        builder.HasData(new GlobalSettings
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            MinimumOrderAmount = 0,
            IsMinimumOrderAmountEnabled = false,
            UpdatedAt = DateTime.UtcNow
        });
    }
}
