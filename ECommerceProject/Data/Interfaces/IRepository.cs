using System.Linq.Expressions;

namespace ECommerceProject.Data.Interfaces
{
    public interface IRepository<T> where T : class
    {
        // Get All
        Task<IEnumerable<T>> GetAllAsync();

        // Get with filter
        Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> filter);

        // Get by ID
        Task<T?> GetByIdAsync(int id);

        // Get single with filter
        Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> filter);

        // Add
        Task AddAsync(T entity);

        // Update
        void Update(T entity);

        // Delete
        void Delete(T entity);

        // Delete Range
        void DeleteRange(IEnumerable<T> entities);

        // Save Changes
        Task<int> SaveAsync();

        // Check if exists
        Task<bool> AnyAsync(Expression<Func<T, bool>> filter);

        // Count
        Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);
    }
}