using CCE.Domain.KnowledgeMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.KnowledgeMaps;

internal sealed class KnowledgeMapNodeConfiguration : IEntityTypeConfiguration<KnowledgeMapNode>
{
    public void Configure(EntityTypeBuilder<KnowledgeMapNode> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.NameAr).HasMaxLength(256).IsRequired();
        builder.Property(n => n.NameEn).HasMaxLength(256).IsRequired();
        builder.Property(n => n.NodeType).HasConversion<int>();
        builder.Property(n => n.IconUrl).HasMaxLength(2048);
        builder.HasIndex(n => new { n.MapId, n.OrderIndex }).HasDatabaseName("ix_km_node_map_order");
    }
}
