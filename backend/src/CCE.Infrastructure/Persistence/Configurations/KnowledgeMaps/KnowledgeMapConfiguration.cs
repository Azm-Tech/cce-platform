using CCE.Domain.KnowledgeMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.KnowledgeMaps;

internal sealed class KnowledgeMapConfiguration : IEntityTypeConfiguration<KnowledgeMap>
{
    public void Configure(EntityTypeBuilder<KnowledgeMap> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.NameAr).HasMaxLength(256).IsRequired();
        builder.Property(m => m.NameEn).HasMaxLength(256).IsRequired();
        builder.Property(m => m.Slug).HasMaxLength(128).IsRequired();
        builder.Property(m => m.RowVersion).IsRowVersion();
        builder.HasIndex(m => m.Slug)
               .IsUnique()
               .HasFilter("[is_deleted] = 0")
               .HasDatabaseName("ux_knowledge_map_slug_active");
        builder.Ignore(m => m.DomainEvents);
    }
}
