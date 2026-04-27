using CCE.Domain.Country;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Country;

internal sealed class CountryResourceRequestConfiguration : IEntityTypeConfiguration<CountryResourceRequest>
{
    public void Configure(EntityTypeBuilder<CountryResourceRequest> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.ProposedTitleAr).HasMaxLength(512).IsRequired();
        builder.Property(r => r.ProposedTitleEn).HasMaxLength(512).IsRequired();
        builder.Property(r => r.ProposedDescriptionAr).HasColumnType("nvarchar(max)");
        builder.Property(r => r.ProposedDescriptionEn).HasColumnType("nvarchar(max)");
        builder.Property(r => r.AdminNotesAr).HasMaxLength(2000);
        builder.Property(r => r.AdminNotesEn).HasMaxLength(2000);
        builder.Property(r => r.ProposedResourceType).HasConversion<int>();
        builder.Property(r => r.Status).HasConversion<int>();
        builder.HasIndex(r => new { r.CountryId, r.Status }).HasDatabaseName("ix_country_request_country_status");
        builder.Ignore(r => r.DomainEvents);
    }
}
