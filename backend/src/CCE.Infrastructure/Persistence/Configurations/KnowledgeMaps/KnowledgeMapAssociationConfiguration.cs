using CCE.Domain.KnowledgeMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.KnowledgeMaps;

internal sealed class KnowledgeMapAssociationConfiguration : IEntityTypeConfiguration<KnowledgeMapAssociation>
{
    public void Configure(EntityTypeBuilder<KnowledgeMapAssociation> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.AssociatedType).HasConversion<int>();
        builder.HasIndex(a => new { a.NodeId, a.AssociatedType, a.AssociatedId })
               .IsUnique()
               .HasDatabaseName("ux_km_assoc_node_type_id");
    }
}
