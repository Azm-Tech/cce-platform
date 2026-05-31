using CCE.Domain.Evaluation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Evaluation;

internal sealed class ServiceEvaluationConfiguration : IEntityTypeConfiguration<ServiceEvaluation>
{
    public void Configure(EntityTypeBuilder<ServiceEvaluation> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.OverallSatisfaction)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.EaseOfUse)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.ContentSuitability)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.Feedback)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.UserId);

        builder.HasIndex(e => e.CreatedOn)
            .HasDatabaseName("ix_service_evaluation_created_on");
    }
}
