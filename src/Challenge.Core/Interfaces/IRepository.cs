using System.Linq.Expressions;

namespace Challenge.Core.Interfaces;

/// <summary>
/// Generic repository abstraction. Demonstrates a reusable, type-agnostic data-access
/// contract shared by all entities (generic approach + SOLID: depend on abstractions).
/// </summary>
public interface IRepository<T> where T : class
{
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default);

    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    void Update(T entity);

    void Remove(T entity);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
