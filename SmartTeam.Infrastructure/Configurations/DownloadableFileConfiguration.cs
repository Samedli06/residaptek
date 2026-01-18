using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Infrastructure.Configurations;

public class DownloadableFileConfiguration : IEntityTypeConfiguration<DownloadableFile>
{
    public void Configure(EntityTypeBuilder<DownloadableFile> builder)
    {
        builder.ToTable("DownloadableFiles");

        builder.HasKey(df => df.Id);

        builder.Property(df => df.Id)
            .ValueGeneratedOnAdd();

        builder.Property(df => df.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(df => df.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(df => df.FilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(df => df.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(df => df.FileSize)
            .IsRequired();

        builder.Property(df => df.Description)
            .HasMaxLength(1000);

        builder.Property(df => df.Category)
            .HasMaxLength(100);

        builder.Property(df => df.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(df => df.DownloadCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(df => df.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(df => df.UpdatedAt);

        builder.Property(df => df.CreatedBy)
            .IsRequired();

        builder.Property(df => df.UpdatedBy);

        // Relationships
        builder.HasOne(df => df.CreatedByUser)
            .WithMany()
            .HasForeignKey(df => df.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(df => df.UpdatedByUser)
            .WithMany()
            .HasForeignKey(df => df.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(df => df.FileName);
        builder.HasIndex(df => df.Category);
        builder.HasIndex(df => df.IsActive);
        builder.HasIndex(df => df.CreatedAt);
        builder.HasIndex(df => df.DownloadCount);
        builder.HasIndex(df => df.ContentType);
    }
}
