using ChatService.Mappers.Interfaces;

namespace ChatService.Mappers
{
    public abstract class BaseEntityMapper<TEntity, TDto> : IEntityMapper<TEntity, TDto>
        where TEntity : class
        where TDto : class
    {
        public abstract TDto MapToDto(TEntity entity, int? userId = null);
        public abstract TEntity MapToEntity(TDto dto);
        public abstract void UpdateEntityFromDto(TDto dto, TEntity entity);

        // Допоміжні методи для null-безпечного мапінгу
        protected TDto? MapToDtoOrNull(TEntity? entity, int? userId = null)
        {
            return entity == null ? null : MapToDto(entity, userId);
        }

        protected TEntity? MapToEntityOrNull(TDto? dto)
        {
            return dto == null ? null : MapToEntity(dto);
        }
        public virtual async Task<TDto> MapToDtoAsync(TEntity entity, int? userId = null)
        {
            // За замовчуванням просто викликаємо синхронний MapToDto
            return MapToDto(entity, userId);
        }

        // Мапінг колекцій
        public virtual IEnumerable<TDto> MapToDtoCollection(IEnumerable<TEntity>? entities, int? userId = null)
        {
            return entities?.Select(e => MapToDto(e,userId)) ?? Enumerable.Empty<TDto>();
        }
        public virtual async Task<IEnumerable<TDto>> MapToDtoCollectionAsync(IEnumerable<TEntity> entities, int? userId = null)
        {
            if (entities == null)
                return Enumerable.Empty<TDto>();

            var tasks = entities.Select(e => MapToDtoAsync(e, userId));
            return await Task.WhenAll(tasks);
        }

        protected IEnumerable<TEntity> MapToEntityCollection(IEnumerable<TDto>? dtos)
        {
            return dtos?.Select(MapToEntity) ?? Enumerable.Empty<TEntity>();
        }
    }
}
