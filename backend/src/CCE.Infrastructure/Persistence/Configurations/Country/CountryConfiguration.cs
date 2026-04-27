using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Country;

internal sealed class CountryConfiguration : IEntityTypeConfiguration<CCE.Domain.Country.Country>
{
    public void Configure(EntityTypeBuilder<CCE.Domain.Country.Country> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.IsoAlpha3).HasMaxLength(3).IsRequired();
        builder.Property(c => c.IsoAlpha2).HasMaxLength(2).IsRequired();
        builder.Property(c => c.NameAr).HasMaxLength(256).IsRequired();
        builder.Property(c => c.NameEn).HasMaxLength(256).IsRequired();
        builder.Property(c => c.RegionAr).HasMaxLength(128).IsRequired();
        builder.Property(c => c.RegionEn).HasMaxLength(128).IsRequired();
        builder.Property(c => c.FlagUrl).HasMaxLength(2048);
        builder.HasIndex(c => c.IsoAlpha3)
               .IsUnique()
               .HasFilter("[is_deleted] = 0")
               .HasDatabaseName("ux_country_iso_alpha3_active");
        builder.HasIndex(c => c.IsoAlpha2).HasDatabaseName("ix_country_iso_alpha2");
        builder.Ignore(c => c.DomainEvents);
    }
}
