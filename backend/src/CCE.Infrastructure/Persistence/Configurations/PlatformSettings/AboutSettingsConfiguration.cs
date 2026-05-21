using CCE.Domain.PlatformSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.PlatformSettings;

internal sealed class AboutSettingsConfiguration : IEntityTypeConfiguration<AboutSettings>
{
    public void Configure(EntityTypeBuilder<AboutSettings> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.DescriptionAr).HasMaxLength(1000).IsRequired();
        builder.Property(s => s.DescriptionEn).HasMaxLength(1000).IsRequired();
        builder.Property(s => s.HowToUseVideoUrl).HasColumnType("nvarchar(max)");
        builder.Property(s => s.RowVersion).IsRowVersion();
        builder.Ignore(s => s.DomainEvents);
    }
}
