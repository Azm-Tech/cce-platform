using CCE.Domain.InteractiveMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.InteractiveMaps;

internal sealed class InteractiveMapConfiguration : IEntityTypeConfiguration<InteractiveMap>
{
    public void Configure(EntityTypeBuilder<InteractiveMap> builder)
    {
        builder.ToTable("interactive_maps");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.NameAr).HasMaxLength(256).IsRequired();
        builder.Property(m => m.NameEn).HasMaxLength(256).IsRequired();
        builder.Property(m => m.DescriptionAr).HasMaxLength(512);
        builder.Property(m => m.DescriptionEn).HasMaxLength(512);
        builder.Property(m => m.Slug).HasMaxLength(128).IsRequired();
        builder.Property(m => m.IsActive).IsRequired();

        builder.HasIndex(m => m.Slug).IsUnique().HasDatabaseName("ux_interactive_map_slug");
    }
}
