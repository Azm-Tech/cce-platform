using CCE.Domain.InteractiveCity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.InteractiveCity;

internal sealed class CityScenarioResultConfiguration : IEntityTypeConfiguration<CityScenarioResult>
{
    public void Configure(EntityTypeBuilder<CityScenarioResult> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.ComputedTotalCostUsd).HasPrecision(18, 2);
        builder.Property(r => r.EngineVersion).HasMaxLength(64).IsRequired();
        builder.HasIndex(r => new { r.ScenarioId, r.ComputedAt }).HasDatabaseName("ix_city_result_scenario_at");
    }
}
