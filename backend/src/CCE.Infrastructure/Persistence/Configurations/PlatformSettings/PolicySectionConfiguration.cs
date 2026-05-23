using CCE.Domain.PlatformSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.PlatformSettings;

internal sealed class PolicySectionConfiguration : IEntityTypeConfiguration<PolicySection>
{
    public void Configure(EntityTypeBuilder<PolicySection> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Type).IsRequired();
        builder.OwnsOne(s => s.Title, title =>
        {
            title.Property(t => t.Ar).HasMaxLength(500).IsRequired();
            title.Property(t => t.En).HasMaxLength(500).IsRequired();
        });
        builder.OwnsOne(s => s.Content, content =>
        {
            content.Property(c => c.Ar).HasColumnType("nvarchar(max)").IsRequired();
            content.Property(c => c.En).HasColumnType("nvarchar(max)").IsRequired();
        });
    }
}
