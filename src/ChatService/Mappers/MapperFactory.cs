using ChatService.Mappers.Interfaces;

namespace ChatService.Mappers
{
    public class MapperFactory : IMapperFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public MapperFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public IEntityMapper<TEntity, TDto> GetMapper<TEntity, TDto>()
            where TEntity : class
            where TDto : class
        {
            return _serviceProvider.GetRequiredService<IEntityMapper<TEntity, TDto>>();
        }
    }
}
