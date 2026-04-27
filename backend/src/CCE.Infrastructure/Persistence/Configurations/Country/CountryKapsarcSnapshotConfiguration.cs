using CCE.Domain.Country;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Country;

internal sealed class CountryKapsarcSnapshotConfiguration : IEntityTypeConfiguration<CountryKapsarcSnapshot>
{
    public void Configure(EntityTypeBuilder<CountryKapsarcSnapshot> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Classification).HasMaxLength(64).IsRequired();
        builder.Property(s => s.PerformanceScore).HasPrecision(5, 2);
        builder.Property(s => s.TotalIndex).HasPrecision(5, 2);
        builder.Property(s => s.SourceVersion).HasMaxLength(32);
        builder.HasIndex(s => new { s.CountryId, s.SnapshotTakenOn })
               .HasDatabaseName("ix_kapsarc_snapshot_country_taken");
    }
}
