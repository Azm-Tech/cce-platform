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
        builder.Property(p => p.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(p => p.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(p => p.LogoUrl).HasColumnType("nvarchar(max)");
        builder.Property(p => p.WebsiteUrl).HasColumnType("nvarchar(max)");
        builder.Property(p => p.DescriptionAr).HasMaxLength(1000);
        builder.Property(p => p.DescriptionEn).HasMaxLength(1000);
        builder.Ignore(p => p.DomainEvents);
    }
}
