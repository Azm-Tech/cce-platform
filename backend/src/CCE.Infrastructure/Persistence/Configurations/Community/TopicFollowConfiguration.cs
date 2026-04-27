using CCE.Domain.Community;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Community;

internal sealed class TopicFollowConfiguration : IEntityTypeConfiguration<TopicFollow>
{
    public void Configure(EntityTypeBuilder<TopicFollow> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.HasIndex(f => new { f.TopicId, f.UserId }).IsUnique().HasDatabaseName("ux_topic_follow_topic_user");
    }
}
