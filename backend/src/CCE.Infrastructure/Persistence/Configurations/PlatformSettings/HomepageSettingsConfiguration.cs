using CCE.Domain.PlatformSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.PlatformSettings;

internal sealed class HomepageSettingsConfiguration : IEntityTypeConfiguration<HomepageSettings>
{
    public void Configure(EntityTypeBuilder<HomepageSettings> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.VideoUrl).HasColumnType("nvarchar(max)");
        builder.Property(s => s.ObjectiveAr).HasMaxLength(1000).IsRequired();
        builder.Property(s => s.ObjectiveEn).HasMaxLength(1000).IsRequired();
        builder.Property(s => s.CceConceptsAr).HasColumnType("nvarchar(max)");
        builder.Property(s => s.CceConceptsEn).HasColumnType("nvarchar(max)");
        builder.Property(s => s.RowVersion).IsRowVersion();
        builder.Ignore(s => s.DomainEvents);
    }
}
