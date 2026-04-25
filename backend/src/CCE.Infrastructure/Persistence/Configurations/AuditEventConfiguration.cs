using CCE.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations;

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events", t => t.HasTrigger("trg_audit_events_no_update_delete"));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();   // application supplies Guid

        builder.Property(e => e.OccurredOn)
            .IsRequired();

        builder.Property(e => e.Actor)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.Action)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.Resource)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(e => e.CorrelationId)
            .IsRequired();

        builder.Property(e => e.Diff)
            // SQL Server: nvarchar(max) for arbitrary JSON
            .HasColumnType("nvarchar(max)");

        // Index on actor + occurred_on for fast "what did user X do?" queries
        builder.HasIndex(e => new { e.Actor, e.OccurredOn })
            .HasDatabaseName("ix_audit_events_actor_occurred_on");

        // Index on correlation_id for incident replay
        builder.HasIndex(e => e.CorrelationId)
            .HasDatabaseName("ix_audit_events_correlation_id");
    }
}
