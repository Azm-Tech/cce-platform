using CCE.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Content;

internal sealed class NewsFollowLogConfiguration : IEntityTypeConfiguration<NewsFollowLog>
{
    public void Configure(EntityTypeBuilder<NewsFollowLog> builder)
    {
        builder.ToTable("news_follow_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.NewsId).IsRequired();
        builder.Property(x => x.Timestamp).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.Timestamp })
            .HasDatabaseName("ix_news_follow_log_user_timestamp");
        builder.HasIndex(x => new { x.UserId, x.NewsId })
            .HasDatabaseName("ix_news_follow_log_user_news");
        builder.HasOne<News>().WithMany().HasForeignKey(x => x.NewsId);
    }
}