using CCE.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Identity;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.FirstName).HasMaxLength(50).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(50).IsRequired();
        builder.Property(u => u.JobTitle).HasMaxLength(50).IsRequired();
        builder.Property(u => u.OrganizationName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LocalePreference).HasMaxLength(2).IsRequired();
        builder.Property(u => u.AvatarUrl).HasMaxLength(2048);
        builder.Property(u => u.KnowledgeLevel).HasConversion<int>();
        builder.Property(u => u.Status).HasConversion<int>();
        builder.HasIndex(u => u.CountryId).HasDatabaseName("ix_users_country_id");
        builder.HasIndex(u => u.CountryCodeId).HasDatabaseName("ix_users_country_code_id");

        // Enforce unique email at the database level to prevent duplicate accounts.
        // Filtered index: only non-null values (Identity allows null emails historically).
        builder.HasIndex(u => u.NormalizedEmail)
            .HasDatabaseName("ix_users_normalized_email_unique")
            .IsUnique()
            .HasFilter("[normalized_email] IS NOT NULL");

        // Sub-11: filtered unique index on EntraIdObjectId. Only enforces uniqueness on
        // non-null values so existing rows pre-cutover (NULL) don't conflict, and so that
        // the lazy-resolver's idempotent linkage stays safe under concurrent first-sign-ins.
        builder.Property(u => u.FollowerCount).HasDefaultValue(0);
        builder.Property(u => u.FollowingCount).HasDefaultValue(0);
        builder.Property(u => u.PostsCount).HasDefaultValue(0);
        builder.Property(u => u.CommentsCount).HasDefaultValue(0);
        builder.HasIndex(u => u.EntraIdObjectId)
            .HasDatabaseName("ix_asp_net_users_entra_id_object_id")
            .IsUnique()
            .HasFilter("[entra_id_object_id] IS NOT NULL");
    }
}
