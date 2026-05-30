using Microsoft.EntityFrameworkCore;
using ECommerceProject.Data.Context;
using ECommerceProject.Data.Interfaces;
using System.Linq.Expressions;

namespace ECommerceProject.Data.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public IQueryable<T> GetQueryable()
        {
            return _dbSet;
        }

        public IQueryable<T> GetQueryable(bool asNoTracking)
        {
            return asNoTracking ? _dbSet.AsNoTracking() : _dbSet;
        }

        private IQueryable<T> ApplyIncludes(IQueryable<T> query, params Expression<Func<T, object>>[] includes)
        {
            if (includes is { Length: > 0 })
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }
            return query;
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes)
        {
            var query = GetQueryable();
            query = ApplyIncludes(query, includes);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAllAsync(bool asNoTracking, params Expression<Func<T, object>>[] includes)
        {
            var query = GetQueryable(asNoTracking);
            query = ApplyIncludes(query, includes);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> filter)
        {
            return await _dbSet.Where(filter).ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> filter, params Expression<Func<T, object>>[] includes)
        {
            var query = _dbSet.Where(filter);
            query = ApplyIncludes(query, includes);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> filter, bool asNoTracking, params Expression<Func<T, object>>[] includes)
        {
            var query = GetQueryable(asNoTracking).Where(filter);
            query = ApplyIncludes(query, includes);
            return await query.ToListAsync();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes)
        {
            var query = GetQueryable();
            query = ApplyIncludes(query, includes);
            return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
        }

        public async Task<T?> GetByIdAsync(int id, bool asNoTracking, params Expression<Func<T, object>>[] includes)
        {
            var query = GetQueryable(asNoTracking);
            query = ApplyIncludes(query, includes);
            return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
        }

        public async Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> filter)
        {
            return await _dbSet.FirstOrDefaultAsync(filter);
        }

        public async Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> filter, params Expression<Func<T, object>>[] includes)
        {
            var query = _dbSet.Where(filter);
            query = ApplyIncludes(query, includes);
            return await query.FirstOrDefaultAsync();
        }

        public async Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> filter, bool asNoTracking, params Expression<Func<T, object>>[] includes)
        {
            var query = GetQueryable(asNoTracking).Where(filter);
            query = ApplyIncludes(query, includes);
            return await query.FirstOrDefaultAsync();
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        public void DeleteRange(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> filter)
        {
            return await _dbSet.AnyAsync(filter);
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
        {
            if (filter == null)
                return await _dbSet.CountAsync();

            return await _dbSet.CountAsync(filter);
        }
    }
}
