using System.Linq.Expressions;
using CCE.Domain.Common;

namespace CCE.Application.Common.Interfaces;

public interface IRepository<T, TId>
    where T : Entity<TId>
    where TId : IEquatable<TId>
{
    Task<T?> GetByIdAsync(TId id, CancellationToken ct = default);

    /// <summary>Load aggregate by id with optional include expression (e.g. q => q.Include(x => x.Tags)).</summary>
    Task<T?> GetByIdAsync(TId id, Func<IQueryable<T>, IQueryable<T>> include, CancellationToken ct = default);

    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Delete(T entity);
}