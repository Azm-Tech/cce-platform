using CCE.Domain.Country;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Country;

internal sealed class CountryProfileConfiguration : IEntityTypeConfiguration<CountryProfile>
{
    public void Configure(EntityTypeBuilder<CountryProfile> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.DescriptionAr).HasColumnType("nvarchar(max)");
        builder.Property(p => p.DescriptionEn).HasColumnType("nvarchar(max)");
        builder.Property(p => p.KeyInitiativesAr).HasColumnType("nvarchar(max)");
        builder.Property(p => p.KeyInitiativesEn).HasColumnType("nvarchar(max)");
        builder.Property(p => p.ContactInfoAr).HasMaxLength(2000);
        builder.Property(p => p.ContactInfoEn).HasMaxLength(2000);
        builder.Property(p => p.RowVersion).IsRowVersion();
        builder.HasIndex(p => p.CountryId).IsUnique().HasDatabaseName("ux_country_profile_country_id");
    }
}
