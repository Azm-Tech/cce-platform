using CCE.Domain.PlatformSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.PlatformSettings;

internal sealed class AboutSettingsConfiguration : IEntityTypeConfiguration<AboutSettings>
{
    public void Configure(EntityTypeBuilder<AboutSettings> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.OwnsOne(s => s.Description, desc =>
        {
            desc.Property(d => d.Ar).HasMaxLength(1000).IsRequired();
            desc.Property(d => d.En).HasMaxLength(1000).IsRequired();
        });
        builder.Property(s => s.HowToUseVideoUrl).HasColumnType("nvarchar(max)");
        builder.Property(s => s.RowVersion).IsRowVersion();
        builder.HasMany(s => s.GlossaryEntries).WithOne().HasForeignKey(e => e.AboutSettingsId);
        builder.HasMany(s => s.KnowledgePartners).WithOne().HasForeignKey(p => p.AboutSettingsId);
        builder.Ignore(s => s.DomainEvents);
    }
}
