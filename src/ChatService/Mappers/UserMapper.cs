using Shared.Protos;
using Shared.DTOs.Identity;
using Shared.DTOs.Message;

namespace ChatService.Mappers
{
    public class UserMapper : BaseEntityMapper<UserData, UserDto>
    {
        private readonly ILogger<UserMapper> _logger;
        public UserMapper(ILogger<UserMapper> logger)
        {
            _logger = logger;
        }
        public override UserDto MapToDto(UserData entity, int? userId = null)
        {
            try
            {
                if (entity == null)
                {
                    _logger.LogError("UserData entity is null in MapToDto method.");
                    throw new ArgumentNullException(nameof(UserData));
                }
                return new UserDto
                {
                    Id = entity.Id,
                    DisplayName = entity.DisplayName ?? string.Empty,
                    UserName = entity.UserName ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping UserData to UserDto");
                throw;
            }
        }

        public override IEnumerable<UserDto> MapToDtoCollection(IEnumerable<UserData>? entities, int? userId = null)
        {
            try
            {
                if (entities == null)
                {
                    _logger.LogWarning("MapToDtoCollection received null entities collection.");
                    return Enumerable.Empty<UserDto>();
                }

                var collection = entities?.Select(e => MapToDto(e)) ?? Enumerable.Empty<UserDto>();
                if (!collection.Any())
                {
                    _logger.LogError("MapToDtoCollection is empty.");
                }
                return collection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при мапінгу колекції UserData до UserDto");
                throw;
            }
        }

        public override async Task<IEnumerable<UserDto>> MapToDtoCollectionAsync(IEnumerable<UserData> entities, int? userId = null)
        {
            try
            {
                if (entities == null)
                {
                    _logger.LogWarning("MapToDtoCollection received null entities collection.");
                    return Enumerable.Empty<UserDto>();
                }

                var tasks = entities.Select(e => MapToDtoAsync(e));
                var results = await Task.WhenAll(tasks);

                if (!results.Any())
                {
                    _logger.LogError("MapToDtoCollection is empty.");
                }
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при мапінгу колекції UserData до UserDto");
                throw;
            }
        }

        [Obsolete("Цей метод не повинен використовуватися в цьому класі", true)]
        public override UserData MapToEntity(UserDto dto)
        {
            throw new NotImplementedException();
        }

        [Obsolete("Цей метод не повинен використовуватися в цьому класі", true)]
        public override void UpdateEntityFromDto(UserDto dto, UserData entity)
        {
            throw new NotImplementedException();
        }
    }
}
