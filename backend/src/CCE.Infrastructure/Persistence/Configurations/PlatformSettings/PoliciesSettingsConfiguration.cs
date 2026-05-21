using CCE.Domain.PlatformSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.PlatformSettings;

internal sealed class PoliciesSettingsConfiguration : IEntityTypeConfiguration<PoliciesSettings>
{
    public void Configure(EntityTypeBuilder<PoliciesSettings> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.RowVersion).IsRowVersion();
        builder.Ignore(s => s.DomainEvents);
    }
}
