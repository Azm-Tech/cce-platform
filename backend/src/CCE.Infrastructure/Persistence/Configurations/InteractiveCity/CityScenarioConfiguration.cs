using CCE.Domain.InteractiveCity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.InteractiveCity;

internal sealed class CityScenarioConfiguration : IEntityTypeConfiguration<CityScenario>
{
    public void Configure(EntityTypeBuilder<CityScenario> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.NameAr).HasMaxLength(256).IsRequired();
        builder.Property(s => s.NameEn).HasMaxLength(256).IsRequired();
        builder.Property(s => s.CityType).HasConversion<int>();
        builder.Property(s => s.ConfigurationJson).HasColumnType("nvarchar(max)");
        builder.HasIndex(s => new { s.UserId, s.LastModifiedOn }).HasDatabaseName("ix_city_scenario_user_modified");
        builder.Ignore(s => s.DomainEvents);
    }
}
