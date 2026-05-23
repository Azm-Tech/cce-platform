using CCE.Domain.Identity;
using CCE.Domain.Verification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Verification;

internal sealed class UserVerificationConfiguration : IEntityTypeConfiguration<UserVerification>
{
    public void Configure(EntityTypeBuilder<UserVerification> builder)
    {
        builder.ToTable("user_verifications");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Contact).HasMaxLength(256).IsRequired();
        builder.Property(e => e.TypeId).IsRequired();
        builder.HasIndex(e => new { e.Contact, e.TypeId }).IsUnique();
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).IsRequired(false);
    }
}
