using CCE.Domain.Community;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Community;

internal sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Title).HasMaxLength(Post.MaxTitleLength);
        builder.Property(p => p.Content).HasMaxLength(Post.MaxContentLength);
        builder.Property(p => p.Locale).HasMaxLength(2).IsRequired();
        builder.Property(p => p.Type).HasConversion<int>();
        builder.Property(p => p.Status).HasConversion<int>();
        builder.HasIndex(p => p.TopicId).HasDatabaseName("ix_post_topic_id");
        builder.HasIndex(p => new { p.CommunityId, p.Score }).IsDescending(false, true).HasDatabaseName("ix_post_community_score");
        builder.HasOne<CCE.Domain.Community.Community>().WithMany()
            .HasForeignKey(p => p.CommunityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => new { p.AuthorId, p.CreatedOn }).HasDatabaseName("ix_post_author_created");
        builder.HasIndex(p => new { p.AuthorId, p.Status }).HasDatabaseName("ix_post_author_status");
        builder.HasIndex(p => p.Score).IsDescending().HasDatabaseName("ix_post_score");
        builder.HasMany(p => p.Tags).WithMany().UsingEntity(j => j.ToTable("post_tag"));
        builder.Property(p => p.ViewCount).HasDefaultValue(0);
        builder.Property(p => p.ShareCount).HasDefaultValue(0);
        builder.Property(p => p.CommentsCount).HasDefaultValue(0);
        builder.HasMany(p => p.Attachments).WithOne().HasForeignKey(a => a.PostId).OnDelete(DeleteBehavior.Cascade);
        builder.Ignore(p => p.DomainEvents);
    }
}
