using CCE.Domain.Community;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations;

internal sealed class ModerationRecordConfiguration : IEntityTypeConfiguration<ModerationRecord>
{
    public void Configure(EntityTypeBuilder<ModerationRecord> builder)
    {
        builder.ToTable("moderation_record");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.ContentType).IsRequired();
        builder.Property(e => e.ContentId).IsRequired();
        builder.Property(e => e.Status).IsRequired();
        builder.Property(e => e.Phase).HasMaxLength(16).IsRequired();
        builder.Property(e => e.Provider).HasMaxLength(64);
        builder.Property(e => e.Category).HasMaxLength(64);
        builder.Property(e => e.Reason).HasMaxLength(512);
        builder.Property(e => e.CreatedOn).IsRequired();

        builder.HasIndex(e => new { e.ContentType, e.ContentId })
            .HasDatabaseName("ix_moderation_record_content");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_moderation_record_status");
    }
}
