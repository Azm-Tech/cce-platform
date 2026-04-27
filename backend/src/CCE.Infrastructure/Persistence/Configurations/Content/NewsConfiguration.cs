using CCE.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Content;

internal sealed class NewsConfiguration : IEntityTypeConfiguration<News>
{
    public void Configure(EntityTypeBuilder<News> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.TitleAr).HasMaxLength(512).IsRequired();
        builder.Property(n => n.TitleEn).HasMaxLength(512).IsRequired();
        builder.Property(n => n.ContentAr).HasColumnType("nvarchar(max)");
        builder.Property(n => n.ContentEn).HasColumnType("nvarchar(max)");
        builder.Property(n => n.Slug).HasMaxLength(256).IsRequired();
        builder.Property(n => n.FeaturedImageUrl).HasMaxLength(2048);
        builder.Property(n => n.RowVersion).IsRowVersion();
        builder.HasIndex(n => n.Slug)
               .IsUnique()
               .HasFilter("[is_deleted] = 0")
               .HasDatabaseName("ux_news_slug_active");
        builder.HasIndex(n => n.PublishedOn).HasDatabaseName("ix_news_published_on");
        builder.Ignore(n => n.DomainEvents);
    }
}
