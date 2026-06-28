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
        builder.Property(n => n.FeaturedImageUrl).HasMaxLength(2048);
        builder.Property(n => n.RowVersion).IsRowVersion();
        builder.HasIndex(n => n.PublishedOn).HasDatabaseName("ix_news_published_on");
        builder.HasIndex(n => n.TopicId).HasDatabaseName("ix_news_topic_id");
        builder.Property(n => n.KnowledgeLevelId).IsRequired(false);
        builder.Property(n => n.JobSectorId).IsRequired(false);

        builder.HasMany(n => n.Tags)
               .WithMany()
               .UsingEntity(j => j.ToTable("news_tag"));

        builder.Ignore(n => n.DomainEvents);
    }
}
