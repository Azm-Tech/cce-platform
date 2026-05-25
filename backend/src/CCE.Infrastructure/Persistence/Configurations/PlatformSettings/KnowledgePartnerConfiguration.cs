using CCE.Domain.PlatformSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.PlatformSettings;

internal sealed class KnowledgePartnerConfiguration : IEntityTypeConfiguration<KnowledgePartner>
{
    public void Configure(EntityTypeBuilder<KnowledgePartner> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.OwnsOne(p => p.Name, name =>
        {
            name.Property(n => n.Ar).HasMaxLength(200).IsRequired();
            name.Property(n => n.En).HasMaxLength(200).IsRequired();
        });
        builder.OwnsOne(p => p.Description, desc =>
        {
            desc.Property(d => d.Ar).HasMaxLength(1000);
            desc.Property(d => d.En).HasMaxLength(1000);
        });
        builder.Property(p => p.LogoUrl).HasColumnType("nvarchar(max)");
        builder.Property(p => p.WebsiteUrl).HasColumnType("nvarchar(max)");
    }
}
