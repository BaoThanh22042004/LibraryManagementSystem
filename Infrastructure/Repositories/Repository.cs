using System.Linq.Expressions;
using Application.Common;
using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly LibraryDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(LibraryDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(object id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IReadOnlyList<T>> ListAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        bool asNoTracking = true,
        params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _dbSet;

        if (predicate != null) query = query.Where(predicate);

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        if (asNoTracking) query = query.AsNoTracking();

        if (orderBy != null) query = orderBy(query);

        return await query.ToListAsync();
    }

    public virtual async Task<PagedResult<T>> PagedListAsync(
        PagedRequest pagedRequest,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        bool asNoTracking = true,
        params Expression<Func<T, object>>[] includes)
    {
        int pageNumber = Math.Max(pagedRequest.PageNumber, 1);
        int pageSize = Math.Clamp(pagedRequest.PageSize, 1, 100);

        IQueryable<T> query = _dbSet;

        if (predicate != null) query = query.Where(predicate);

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        if (asNoTracking) query = query.AsNoTracking();

        if (orderBy != null) query = orderBy(query);

        int total = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<T>(items, total, pageNumber, pageSize);
    }

    public virtual async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public virtual async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    public virtual void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public virtual void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }

    public virtual async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    public virtual async Task<T?> GetAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
	}

	public virtual IQueryable<T> Query()
    {
        return _dbSet.AsQueryable();
    }
}
