using CCE.Domain.InteractiveMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.InteractiveMaps;

internal sealed class InteractiveMapNodeConfiguration : IEntityTypeConfiguration<InteractiveMapNode>
{
    public void Configure(EntityTypeBuilder<InteractiveMapNode> builder)
    {
        builder.ToTable("interactive_map_nodes");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.InteractiveMapId).IsRequired();
        builder.Property(n => n.NameAr).HasMaxLength(256).IsRequired();
        builder.Property(n => n.NameEn).HasMaxLength(256).IsRequired();
        builder.Property(n => n.IconKey).HasMaxLength(128).IsRequired();
        builder.Property(n => n.Category);
        builder.Property(n => n.CategoryNameAr).HasMaxLength(128);
        builder.Property(n => n.CategoryNameEn).HasMaxLength(128);
        builder.Property(n => n.Level).IsRequired();
        builder.Property(n => n.TopicSlug).HasMaxLength(128);
        builder.Property(n => n.IsActive).IsRequired();

        builder.HasIndex(n => n.InteractiveMapId).HasDatabaseName("ix_interactive_map_node_map_id");
        builder.HasIndex(n => n.ParentId).HasDatabaseName("ix_interactive_map_node_parent_id");
        builder.HasIndex(n => n.TopicId).HasDatabaseName("ix_interactive_map_node_topic_id");
    }
}
