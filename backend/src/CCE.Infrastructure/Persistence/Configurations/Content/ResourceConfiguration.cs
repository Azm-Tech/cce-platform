using CCE.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Content;

internal sealed class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.TitleAr).HasMaxLength(512).IsRequired();
        builder.Property(r => r.TitleEn).HasMaxLength(512).IsRequired();
        builder.Property(r => r.DescriptionAr).HasColumnType("nvarchar(max)");
        builder.Property(r => r.DescriptionEn).HasColumnType("nvarchar(max)");
        builder.Property(r => r.ResourceType).HasConversion<int>();
        builder.Property(r => r.RowVersion).IsRowVersion();
        builder.HasIndex(r => new { r.CategoryId, r.PublishedOn }).HasDatabaseName("ix_resource_category_published");
        builder.HasIndex(r => r.CountryId).HasDatabaseName("ix_resource_country_id");
        builder.HasIndex(r => r.AssetFileId).HasDatabaseName("ix_resource_asset_file_id");
        builder.Ignore(r => r.DomainEvents);
    }
}
