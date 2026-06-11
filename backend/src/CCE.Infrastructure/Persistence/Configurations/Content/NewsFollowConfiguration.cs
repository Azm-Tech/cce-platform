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
        builder.Property(f => f.Status)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(f => f.UnfollowedOn);
        builder.Property(f => f.CreatedOn)
            .HasColumnName("followed_on")
            .IsRequired();
        builder.HasIndex(f => f.UserId).IsUnique().HasDatabaseName("ux_news_follow_user");
    }
}
