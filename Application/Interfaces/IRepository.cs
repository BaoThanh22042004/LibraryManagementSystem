using System.Linq.Expressions;
using Application.Common;

namespace Application.Interfaces;

public interface IRepository<T> where T : class
{
    Task<IReadOnlyList<T>> ListAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        bool asNoTracking = true,
        params Expression<Func<T, object>>[] includes);
    Task<PagedResult<T>> PagedListAsync(
        PagedRequest pagedRequest,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        bool asNoTracking = true,
        params Expression<Func<T, object>>[] includes);
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    void Update(T entity);
    void Delete(T entity);
    Task<int> SaveChangesAsync();
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    Task<T?> GetAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

    IQueryable<T> Query(); // Expose IQueryable when needed
}
