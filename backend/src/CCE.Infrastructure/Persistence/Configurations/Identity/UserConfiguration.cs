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
    }
}
