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
        builder.Property(p => p.Content).HasMaxLength(8000).IsRequired();
        builder.Property(p => p.Locale).HasMaxLength(2).IsRequired();
        builder.HasIndex(p => p.TopicId).HasDatabaseName("ix_post_topic_id");
        builder.HasIndex(p => new { p.AuthorId, p.CreatedOn }).HasDatabaseName("ix_post_author_created");
        builder.Ignore(p => p.DomainEvents);
    }
}
