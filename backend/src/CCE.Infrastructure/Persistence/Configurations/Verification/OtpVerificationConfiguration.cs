using CCE.Domain.Verification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Verification;

internal sealed class OtpVerificationConfiguration : IEntityTypeConfiguration<OtpVerification>
{
    public void Configure(EntityTypeBuilder<OtpVerification> builder)
    {
        builder.ToTable("otp_verifications");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Contact).HasMaxLength(256).IsRequired();
        builder.Property(e => e.TypeId).IsRequired();
        builder.Property(e => e.CodeHash).HasMaxLength(512).IsRequired();
        builder.Property(e => e.ExtraData).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(e => e.UserId).IsRequired(false);
        builder.HasIndex(e => new { e.Contact, e.TypeId });
        builder.HasIndex(e => new { e.UserId, e.Contact, e.TypeId })
            .HasDatabaseName("ix_otp_verifications_user_contact_type");
    }
}
