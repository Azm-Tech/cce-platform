using CCE.Domain.Community;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Community;

internal sealed class PostFollowConfiguration : IEntityTypeConfiguration<PostFollow>
{
    public void Configure(EntityTypeBuilder<PostFollow> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.HasIndex(f => new { f.PostId, f.UserId }).IsUnique().HasDatabaseName("ux_post_follow_post_user");
    }
}
