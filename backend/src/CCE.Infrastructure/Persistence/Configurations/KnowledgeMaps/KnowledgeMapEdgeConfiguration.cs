using CCE.Domain.KnowledgeMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.KnowledgeMaps;

internal sealed class KnowledgeMapEdgeConfiguration : IEntityTypeConfiguration<KnowledgeMapEdge>
{
    public void Configure(EntityTypeBuilder<KnowledgeMapEdge> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.RelationshipType).HasConversion<int>();
        builder.HasIndex(e => new { e.MapId, e.FromNodeId, e.ToNodeId, e.RelationshipType })
               .IsUnique()
               .HasDatabaseName("ux_km_edge_map_from_to_relation");
        builder.HasIndex(e => e.FromNodeId).HasDatabaseName("ix_km_edge_from_node");
        builder.HasIndex(e => e.ToNodeId).HasDatabaseName("ix_km_edge_to_node");
    }
}
