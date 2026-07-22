using System.Linq.Expressions;
using Challenge.Core.Interfaces;
using Challenge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Challenge.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the generic repository. A single implementation serves
/// every entity type, keeping data access DRY.
/// </summary>
public class Repository<T>(AppDbContext context) : IRepository<T> where T : class
{
    private readonly DbSet<T> _set = context.Set<T>();

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _set.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await _set.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    public async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        => await _set.FindAsync(new[] { id }, cancellationToken).AsTask();

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _set.AddAsync(entity, cancellationToken);
        return entity;
    }

    public void Update(T entity) => _set.Update(entity);

    public void Remove(T entity) => _set.Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}
