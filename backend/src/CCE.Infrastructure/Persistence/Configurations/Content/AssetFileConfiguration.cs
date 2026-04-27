using CCE.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Content;

internal sealed class AssetFileConfiguration : IEntityTypeConfiguration<AssetFile>
{
    public void Configure(EntityTypeBuilder<AssetFile> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Url).HasMaxLength(2048).IsRequired();
        builder.Property(a => a.OriginalFileName).HasMaxLength(512).IsRequired();
        builder.Property(a => a.MimeType).HasMaxLength(128).IsRequired();
        builder.Property(a => a.VirusScanStatus).HasConversion<int>();
        builder.HasIndex(a => a.VirusScanStatus).HasDatabaseName("ix_asset_file_scan_status");
    }
}
