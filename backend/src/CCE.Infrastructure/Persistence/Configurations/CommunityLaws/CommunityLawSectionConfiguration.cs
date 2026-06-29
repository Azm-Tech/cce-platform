using CCE.Domain.CommunityLaws;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.CommunityLaws;

internal sealed class CommunityLawSectionConfiguration : IEntityTypeConfiguration<CommunityLawSection>
{
    public void Configure(EntityTypeBuilder<CommunityLawSection> builder)
    {
        builder.ToTable("community_law_sections");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.OrderIndex)
            .IsRequired();

        builder.HasIndex(e => e.OrderIndex)
            .HasDatabaseName("ix_community_law_section_order");

        builder.OwnsOne(e => e.Title, nav =>
        {
            nav.Property(t => t.Ar).IsRequired().HasColumnName("title_ar");
            nav.Property(t => t.En).IsRequired().HasColumnName("title_en");
        });

        builder.OwnsOne(e => e.Content, nav =>
        {
            nav.Property(t => t.Ar).IsRequired().HasColumnName("content_ar");
            nav.Property(t => t.En).IsRequired().HasColumnName("content_en");
        });
    }
}
