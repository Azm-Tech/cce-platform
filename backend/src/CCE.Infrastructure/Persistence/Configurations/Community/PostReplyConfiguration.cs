using CCE.Domain.Community;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Community;

internal sealed class PostReplyConfiguration : IEntityTypeConfiguration<PostReply>
{
    public void Configure(EntityTypeBuilder<PostReply> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Content).HasMaxLength(8000).IsRequired();
        builder.Property(r => r.Locale).HasMaxLength(2).IsRequired();
        builder.HasIndex(r => r.PostId).HasDatabaseName("ix_post_reply_post_id");
        builder.HasIndex(r => r.ParentReplyId).HasDatabaseName("ix_post_reply_parent_id");
    }
}
