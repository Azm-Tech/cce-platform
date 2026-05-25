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
        builder.HasIndex(e => new { e.Contact, e.TypeId });
    }
}
