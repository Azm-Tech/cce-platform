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
        builder.Property(s => s.TitleAr).HasMaxLength(500).IsRequired();
        builder.Property(s => s.TitleEn).HasMaxLength(500).IsRequired();
        builder.Property(s => s.ContentAr).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(s => s.ContentEn).HasColumnType("nvarchar(max)").IsRequired();
        builder.Ignore(s => s.DomainEvents);
    }
}
