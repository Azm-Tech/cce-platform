using CCE.Domain.PlatformSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.PlatformSettings;

internal sealed class HomepageCountryConfiguration : IEntityTypeConfiguration<HomepageCountry>
{
    public void Configure(EntityTypeBuilder<HomepageCountry> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.HasIndex(c => new { c.HomepageSettingsId, c.CountryId })
               .IsUnique()
               .HasDatabaseName("ix_homepage_country_settings_country");
    }
}
