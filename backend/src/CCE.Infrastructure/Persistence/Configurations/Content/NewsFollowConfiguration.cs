using CCE.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Content;

internal sealed class NewsFollowConfiguration : IEntityTypeConfiguration<NewsFollow>
{
    public void Configure(EntityTypeBuilder<NewsFollow> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.HasIndex(f => f.UserId).IsUnique().HasDatabaseName("ux_news_follow_user");
    }
}
