using CCE.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Content;

internal sealed class ResourceCategoryConfiguration : IEntityTypeConfiguration<ResourceCategory>
{
    public void Configure(EntityTypeBuilder<ResourceCategory> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.NameAr).HasMaxLength(256).IsRequired();
        builder.Property(c => c.NameEn).HasMaxLength(256).IsRequired();
        builder.Property(c => c.Slug).HasMaxLength(128).IsRequired();
        builder.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("ux_resource_category_slug");
        builder.HasIndex(c => c.ParentId).HasDatabaseName("ix_resource_category_parent_id");
    }
}
