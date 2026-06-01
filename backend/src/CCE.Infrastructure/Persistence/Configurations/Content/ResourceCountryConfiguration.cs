using CCE.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Content;

internal sealed class ResourceCountryConfiguration : IEntityTypeConfiguration<ResourceCountry>
{
    public void Configure(EntityTypeBuilder<ResourceCountry> builder)
    {
        builder.HasKey(rc => new { rc.ResourceId, rc.CountryId });
        builder.Property(rc => rc.ResourceId).ValueGeneratedNever();
        builder.Property(rc => rc.CountryId).ValueGeneratedNever();
        builder.HasIndex(rc => rc.CountryId).HasDatabaseName("ix_resource_country_country_id");
    }
}
