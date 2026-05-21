using CCE.Domain.PlatformSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.PlatformSettings;

internal sealed class GlossaryEntryConfiguration : IEntityTypeConfiguration<GlossaryEntry>
{
    public void Configure(EntityTypeBuilder<GlossaryEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.TermAr).HasMaxLength(100).IsRequired();
        builder.Property(e => e.TermEn).HasMaxLength(100).IsRequired();
        builder.Property(e => e.DefinitionAr).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.DefinitionEn).HasMaxLength(1000).IsRequired();
        builder.Ignore(e => e.DomainEvents);
    }
}
