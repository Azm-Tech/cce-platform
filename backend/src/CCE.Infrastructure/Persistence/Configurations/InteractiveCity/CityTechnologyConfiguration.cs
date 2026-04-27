using CCE.Domain.InteractiveCity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.InteractiveCity;

internal sealed class CityTechnologyConfiguration : IEntityTypeConfiguration<CityTechnology>
{
    public void Configure(EntityTypeBuilder<CityTechnology> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.NameAr).HasMaxLength(256).IsRequired();
        builder.Property(t => t.NameEn).HasMaxLength(256).IsRequired();
        builder.Property(t => t.CategoryAr).HasMaxLength(128).IsRequired();
        builder.Property(t => t.CategoryEn).HasMaxLength(128).IsRequired();
        builder.Property(t => t.IconUrl).HasMaxLength(2048);
        builder.Property(t => t.CarbonImpactKgPerYear).HasPrecision(18, 2);
        builder.Property(t => t.CostUsd).HasPrecision(18, 2);
        builder.HasIndex(t => t.IsActive).HasDatabaseName("ix_city_tech_is_active");
    }
}
