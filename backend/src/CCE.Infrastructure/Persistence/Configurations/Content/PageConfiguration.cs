using CCE.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Content;

internal sealed class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Slug).HasMaxLength(256).IsRequired();
        builder.Property(p => p.PageType).HasConversion<int>();
        builder.Property(p => p.TitleAr).HasMaxLength(512).IsRequired();
        builder.Property(p => p.TitleEn).HasMaxLength(512).IsRequired();
        builder.Property(p => p.ContentAr).HasColumnType("nvarchar(max)");
        builder.Property(p => p.ContentEn).HasColumnType("nvarchar(max)");
        builder.Property(p => p.RowVersion).IsRowVersion();
        builder.HasIndex(p => new { p.PageType, p.Slug })
               .IsUnique()
               .HasFilter("[is_deleted] = 0")
               .HasDatabaseName("ux_page_type_slug_active");
        builder.Ignore(p => p.DomainEvents);
    }
}
