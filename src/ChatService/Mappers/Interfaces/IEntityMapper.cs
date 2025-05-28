namespace ChatService.Mappers.Interfaces
{
    public interface IEntityMapper<TEntity, TDto>
    {
        // Перетворення з сутності в DTO
        TDto MapToDto(TEntity entity, int? userId = null);

        // Перетворення з DTO в сутність
        TEntity MapToEntity(TDto dto);

        // Оновлення сутності даними з DTO
        void UpdateEntityFromDto(TDto dto, TEntity entity);
        Task<TDto> MapToDtoAsync(TEntity entity, int? userId = null);
        Task<IEnumerable<TDto>> MapToDtoCollectionAsync(IEnumerable<TEntity> entities, int? userId = null);
        IEnumerable<TDto> MapToDtoCollection(IEnumerable<TEntity> entities, int? userId = null);
    }
}
