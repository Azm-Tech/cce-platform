using CCE.Domain.Common;

namespace CCE.Application.Common.Interfaces;

public interface IRepository<T, TId>
    where T : AggregateRoot<TId>
    where TId : IEquatable<TId>
{
    Task<T?> GetByIdAsync(TId id, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Delete(T entity);
}