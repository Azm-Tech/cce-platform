using CCE.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Identity;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.LocalePreference).HasMaxLength(2).IsRequired();
        builder.Property(u => u.AvatarUrl).HasMaxLength(2048);
        builder.Property(u => u.Interests).HasColumnType("nvarchar(max)");
        builder.Property(u => u.KnowledgeLevel).HasConversion<int>();
        builder.HasIndex(u => u.CountryId).HasDatabaseName("ix_users_country_id");

        // Sub-11: filtered unique index on EntraIdObjectId. Only enforces uniqueness on
        // non-null values so existing rows pre-cutover (NULL) don't conflict, and so that
        // the lazy-resolver's idempotent linkage stays safe under concurrent first-sign-ins.
        builder.HasIndex(u => u.EntraIdObjectId)
            .HasDatabaseName("ix_asp_net_users_entra_id_object_id")
            .IsUnique()
            .HasFilter("[entra_id_object_id] IS NOT NULL");
    }
}
