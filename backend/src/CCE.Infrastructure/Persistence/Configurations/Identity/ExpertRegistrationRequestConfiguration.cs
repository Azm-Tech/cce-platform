using CCE.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Identity;

internal sealed class ExpertRegistrationRequestConfiguration
    : IEntityTypeConfiguration<ExpertRegistrationRequest>
{
    public void Configure(EntityTypeBuilder<ExpertRegistrationRequest> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.RequestedBioAr).HasMaxLength(2000);
        builder.Property(r => r.RequestedBioEn).HasMaxLength(2000);
        builder.Property(r => r.RequestedTags).HasColumnType("nvarchar(max)");
        builder.Property(r => r.RejectionReasonAr).HasMaxLength(1000);
        builder.Property(r => r.RejectionReasonEn).HasMaxLength(1000);
        builder.Property(r => r.Status).HasConversion<int>();
        builder.HasIndex(r => r.RequestedById).HasDatabaseName("ix_expert_request_requested_by");
        builder.HasIndex(r => r.Status).HasDatabaseName("ix_expert_request_status");
        builder.Ignore(r => r.DomainEvents);
    }
}
