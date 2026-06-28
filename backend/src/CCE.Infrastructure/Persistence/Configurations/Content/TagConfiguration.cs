using CCE.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Content;

internal sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.NameAr).HasMaxLength(128).IsRequired();
        builder.Property(t => t.NameEn).HasMaxLength(128).IsRequired();
        builder.Property(t => t.Color).HasMaxLength(7);
        builder.HasIndex(t => t.NameEn).IsUnique().HasDatabaseName("ux_tag_name_en");
    }
}
