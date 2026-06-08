using CCE.Domain.Community;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Community;

internal sealed class MentionConfiguration : IEntityTypeConfiguration<Mention>
{
    public void Configure(EntityTypeBuilder<Mention> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.SourceType).HasConversion<int>();
        builder.HasIndex(m => new { m.SourceType, m.SourceId, m.MentionedUserId }).IsUnique()
            .HasDatabaseName("ux_mention_source_user");
        builder.HasIndex(m => new { m.MentionedUserId, m.CreatedOn }).HasDatabaseName("ix_mention_user_created");
    }
}
