using CCE.Application.Common.Interfaces;
using CCE.Domain.Audit;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Persistence;

/// <summary>
/// Application <see cref="DbContext"/>. Configured via <see cref="DependencyInjection"/> to use
/// SQL Server with snake_case naming. Foundation contains exactly one DbSet (audit events);
/// sub-project 2 expands the schema to the full BRD entity set.
/// </summary>
public sealed class CceDbContext : DbContext, ICceDbContext
{
    public CceDbContext(DbContextOptions<CceDbContext> options) : base(options) { }

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CceDbContext).Assembly);
    }
}
