using CCE.Application.Common.Interfaces;
using CCE.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Persistence;

public class Repository<T, TId> : IRepository<T, TId>
    where T : AggregateRoot<TId>
    where TId : IEquatable<TId>
{
    protected CceDbContext Db { get; }

    public Repository(CceDbContext db) => Db = db;

    public virtual async Task<T?> GetByIdAsync(TId id, CancellationToken ct)
        => await Db.Set<T>().FindAsync(new object[] { id }, ct).ConfigureAwait(false);

    public virtual async Task AddAsync(T entity, CancellationToken ct)
        => await Db.Set<T>().AddAsync(entity, ct).ConfigureAwait(false);

    public virtual void Update(T entity)
    {
        if (Db.Entry(entity).State == EntityState.Detached)
        {
            Db.Set<T>().Attach(entity);
            Db.Entry(entity).State = EntityState.Modified;
        }
    }

    public virtual void Delete(T entity)
        => Db.Set<T>().Remove(entity);
}