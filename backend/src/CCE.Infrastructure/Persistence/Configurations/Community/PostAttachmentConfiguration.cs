using CCE.Domain.Community;
using CCE.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Community;

internal sealed class PostAttachmentConfiguration : IEntityTypeConfiguration<PostAttachment>
{
    public void Configure(EntityTypeBuilder<PostAttachment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Kind).HasConversion<int>();
        builder.HasIndex(a => new { a.PostId, a.SortOrder }).HasDatabaseName("ix_post_attachment_post_sort");
        builder.HasOne<AssetFile>().WithMany().HasForeignKey(a => a.AssetFileId).OnDelete(DeleteBehavior.Restrict);
    }
}
