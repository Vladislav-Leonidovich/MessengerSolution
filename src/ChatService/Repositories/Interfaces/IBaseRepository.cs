using System.Linq.Expressions;

namespace ChatService.Repositories.Interfaces
{
    public interface IBaseRepository<TEntity, TKey> where TEntity : class
    {
        // Отримати сутність за ідентифікатором
        Task<TEntity> GetByIdAsync(TKey id);

        // Отримати всі сутності
        Task<IEnumerable<TEntity>> GetAllAsync();

        // Отримати сутності за умовою
        Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);

        // Додати сутність
        Task<TEntity> AddAsync(TEntity entity);

        // Оновити сутність
        Task<bool> UpdateAsync(TEntity entity);

        // Видалити сутність
        Task<bool> DeleteAsync(TKey id);

        // Перевірити, чи існує сутність
        Task<bool> ExistsAsync(TKey id);
    }
}
