using CCE.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations;

internal sealed class PermissionAuditLogConfiguration : IEntityTypeConfiguration<PermissionAuditLog>
{
    public void Configure(EntityTypeBuilder<PermissionAuditLog> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).UseIdentityColumn();
        builder.Property(p => p.ChangedByEmail).HasMaxLength(256).IsRequired();
        builder.Property(p => p.RoleName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.PermissionName).HasMaxLength(200).IsRequired();
        // No FK — audit rows must survive role/user deletions.
    }
}
