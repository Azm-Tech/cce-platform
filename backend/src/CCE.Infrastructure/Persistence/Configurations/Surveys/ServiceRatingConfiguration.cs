using CCE.Domain.Surveys;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Surveys;

internal sealed class ServiceRatingConfiguration : IEntityTypeConfiguration<ServiceRating>
{
    public void Configure(EntityTypeBuilder<ServiceRating> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Page).HasMaxLength(256).IsRequired();
        builder.Property(r => r.Locale).HasMaxLength(2).IsRequired();
        builder.Property(r => r.CommentAr).HasMaxLength(2000);
        builder.Property(r => r.CommentEn).HasMaxLength(2000);
        builder.HasIndex(r => r.SubmittedOn).HasDatabaseName("ix_service_rating_submitted_on");
    }
}
