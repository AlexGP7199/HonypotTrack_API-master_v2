using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using HoneypotTrack.Infrastrcture.Persistences.Context;
using HoneypotTrack.Infrastrcture.Persistences.Interfaces;

namespace HoneypotTrack.Infrastrcture.Persistences.Repositories;

/// <summary>
/// Repositorio genérico que usa IDataContextProvider para resolver el DbContext correcto.
/// - Usuario legítimo → AppDbContext (datos reales)
/// - Atacante honeypot → HoneypotDbContext (datos falsos)
/// </summary>
public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    private readonly DbContext _context;
    private readonly DbSet<T> _dbSet;

    public GenericRepository(IDataContextProvider contextProvider)
    {
        _context = contextProvider.GetDataContext();
        _dbSet = _context.Set<T>();
    }

    // Queries
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.AsNoTracking().ToListAsync();
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AsNoTracking().Where(predicate).ToListAsync();
    }

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate);
    }

    public IQueryable<T> GetQueryable()
    {
        return _dbSet.AsNoTracking().AsQueryable();
    }

    // Commands
    public async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }

    public void RemoveRange(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    // Utilities
    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        return predicate is null
            ? await _dbSet.CountAsync()
            : await _dbSet.CountAsync(predicate);
    }
}
