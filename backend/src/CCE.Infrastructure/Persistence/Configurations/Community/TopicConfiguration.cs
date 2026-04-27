using CCE.Domain.Community;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Community;

internal sealed class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.NameAr).HasMaxLength(256).IsRequired();
        builder.Property(t => t.NameEn).HasMaxLength(256).IsRequired();
        builder.Property(t => t.Slug).HasMaxLength(128).IsRequired();
        builder.Property(t => t.IconUrl).HasMaxLength(2048);
        builder.HasIndex(t => t.Slug)
               .IsUnique()
               .HasFilter("[is_deleted] = 0")
               .HasDatabaseName("ux_topic_slug_active");
    }
}
