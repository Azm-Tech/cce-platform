using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Country;

internal sealed class CountryConfiguration : IEntityTypeConfiguration<CCE.Domain.Country.Country>
{
    public void Configure(EntityTypeBuilder<CCE.Domain.Country.Country> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.IsoAlpha3).HasMaxLength(3);
        builder.Property(c => c.IsoAlpha2).HasMaxLength(2);
        builder.Property(c => c.NameAr).HasMaxLength(256).IsRequired();
        builder.Property(c => c.NameEn).HasMaxLength(256).IsRequired();
        builder.Property(c => c.RegionAr).HasMaxLength(128);
        builder.Property(c => c.RegionEn).HasMaxLength(128);
        builder.Property(c => c.FlagUrl).HasMaxLength(2048);
        builder.Property(c => c.DialCode).HasMaxLength(16);
        builder.Property(c => c.IsCceCountry).IsRequired().HasDefaultValue(false);
        builder.HasIndex(c => c.IsoAlpha3)
               .IsUnique()
               .HasFilter("[is_deleted] = 0 AND [is_cce_country] = 1")
               .HasDatabaseName("ux_country_iso_alpha3_active");
        builder.HasIndex(c => c.IsoAlpha2).HasDatabaseName("ix_country_iso_alpha2");
        builder.HasIndex(c => c.DialCode)
               .HasFilter("[dial_code] IS NOT NULL")
               .HasDatabaseName("ix_country_dial_code");
        builder.Ignore(c => c.DomainEvents);
    }
}
