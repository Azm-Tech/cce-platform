using CCE.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Content;

internal sealed class HomepageSectionConfiguration : IEntityTypeConfiguration<HomepageSection>
{
    public void Configure(EntityTypeBuilder<HomepageSection> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.SectionType).HasConversion<int>();
        builder.Property(s => s.ContentAr).HasColumnType("nvarchar(max)");
        builder.Property(s => s.ContentEn).HasColumnType("nvarchar(max)");
        builder.HasIndex(s => new { s.IsActive, s.OrderIndex })
               .HasDatabaseName("ix_homepage_section_active_order");
    }
}
