namespace ChatService.Mappers.Interfaces
{
    public interface IMapperFactory
    {
        IEntityMapper<TEntity, TDto> GetMapper<TEntity, TDto>()
            where TEntity : class
            where TDto : class;
    }
}
