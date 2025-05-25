using ChatService.Mappers.Interfaces;

namespace ChatService.Mappers
{
    public abstract class BaseEntityMapper<TEntity, TDto> : IEntityMapper<TEntity, TDto>
        where TEntity : class
        where TDto : class
    {
        public abstract TDto MapToDto(TEntity entity);
        public abstract TEntity MapToEntity(TDto dto);
        public abstract void UpdateEntityFromDto(TDto dto, TEntity entity);

        // Допоміжні методи для null-безпечного мапінгу
        protected TDto? MapToDtoOrNull(TEntity? entity)
        {
            return entity == null ? null : MapToDto(entity);
        }

        protected TEntity? MapToEntityOrNull(TDto? dto)
        {
            return dto == null ? null : MapToEntity(dto);
        }

        // Мапінг колекцій
        protected IEnumerable<TDto> MapToDtoCollection(IEnumerable<TEntity>? entities)
        {
            return entities?.Select(MapToDto) ?? Enumerable.Empty<TDto>();
        }

        protected IEnumerable<TEntity> MapToEntityCollection(IEnumerable<TDto>? dtos)
        {
            return dtos?.Select(MapToEntity) ?? Enumerable.Empty<TEntity>();
        }
    }
}
