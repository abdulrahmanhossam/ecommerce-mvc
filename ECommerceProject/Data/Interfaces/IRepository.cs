using System.Linq.Expressions;

namespace ECommerceProject.Data.Interfaces
{
    public interface IRepository<T> where T : class
    {
        IQueryable<T> GetQueryable();

        IQueryable<T> GetQueryable(bool asNoTracking);

        Task<IEnumerable<T>> GetAllAsync();

        Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);

        Task<IEnumerable<T>> GetAllAsync(bool asNoTracking, params Expression<Func<T, object>>[] includes);

        Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> filter);

        Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> filter, params Expression<Func<T, object>>[] includes);

        Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> filter, bool asNoTracking, params Expression<Func<T, object>>[] includes);

        Task<T?> GetByIdAsync(int id);

        Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes);

        Task<T?> GetByIdAsync(int id, bool asNoTracking, params Expression<Func<T, object>>[] includes);

        Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> filter);

        Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> filter, params Expression<Func<T, object>>[] includes);

        Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> filter, bool asNoTracking, params Expression<Func<T, object>>[] includes);

        Task AddAsync(T entity);

        void Update(T entity);

        void Delete(T entity);

        void DeleteRange(IEnumerable<T> entities);

        Task<int> SaveAsync();

        Task<bool> AnyAsync(Expression<Func<T, bool>> filter);

        Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);
    }
}
