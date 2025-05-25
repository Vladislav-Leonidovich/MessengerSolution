namespace ChatService.Mappers.Interfaces
{
    public interface IEntityMapper<TEntity, TDto>
    {
        // Перетворення з сутності в DTO
        TDto MapToDto(TEntity entity);

        // Перетворення з DTO в сутність
        TEntity MapToEntity(TDto dto);

        // Оновлення сутності даними з DTO
        void UpdateEntityFromDto(TDto dto, TEntity entity);
    }
}
