using CCE.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Media;

internal sealed class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(m => m.Url).HasMaxLength(2048).IsRequired();
        builder.Property(m => m.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(m => m.MimeType).HasMaxLength(100).IsRequired();
        builder.Property(m => m.TitleAr).HasMaxLength(200);
        builder.Property(m => m.TitleEn).HasMaxLength(200);
        builder.Property(m => m.DescriptionAr).HasMaxLength(1000);
        builder.Property(m => m.DescriptionEn).HasMaxLength(1000);
        builder.Property(m => m.AltTextAr).HasMaxLength(500);
        builder.Property(m => m.AltTextEn).HasMaxLength(500);
    }
}
