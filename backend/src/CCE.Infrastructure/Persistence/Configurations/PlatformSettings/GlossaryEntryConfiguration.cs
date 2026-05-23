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
        builder.OwnsOne(e => e.Term, term =>
        {
            term.Property(t => t.Ar).HasMaxLength(100).IsRequired();
            term.Property(t => t.En).HasMaxLength(100).IsRequired();
        });
        builder.OwnsOne(e => e.Definition, def =>
        {
            def.Property(d => d.Ar).HasMaxLength(1000).IsRequired();
            def.Property(d => d.En).HasMaxLength(1000).IsRequired();
        });
    }
}
