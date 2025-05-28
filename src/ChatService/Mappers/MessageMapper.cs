using MassTransit;
using Shared.DTOs.Chat;
using Shared.DTOs.Message;
using Shared.Protos;

namespace ChatService.Mappers
{
    public class MessageMapper : BaseEntityMapper<MessageData, MessageDto>
    {
        private readonly ILogger<MessageMapper> _logger;
        public MessageMapper(ILogger<MessageMapper> logger) 
        {
            _logger = logger;
        }

        public override MessageDto MapToDto(MessageData entity, int? userId = null)
        {
            try
            {
                if(entity == null)
                {
                    _logger.LogError("MessageData entity is null in MapToDto method.");
                    throw new ArgumentNullException(nameof(MessageData));
                }
                return new MessageDto
                {
                    Id = entity.Id,
                    ChatRoomId = entity.ChatRoomId,
                    SenderUserId = entity.SenderUserId,
                    Content = entity.Content,
                    CreatedAt = entity.CreatedAt.ToDateTime(),
                    IsRead = entity.IsRead,
                    ReadAt = entity.ReadAt?.ToDateTime(),
                    IsEdited = entity.IsEdited,
                    EditedAt = entity.EditedAt?.ToDateTime(),
                    Status = (Shared.DTOs.Common.MessageStatus)entity.Status
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping MessageData to MessageDto");
                throw;
            }
        }

        public override IEnumerable<MessageDto> MapToDtoCollection(IEnumerable<MessageData>? entities, int? userId = null)
        {
            try
            {
                if (entities == null)
                {
                    _logger.LogWarning("MapToDtoCollection received null entities collection.");
                    return Enumerable.Empty<MessageDto>();
                }

                var collection = entities?.Select(e => MapToDto(e)) ?? Enumerable.Empty<MessageDto>();
                if (!collection.Any())
                {
                    _logger.LogError("MapToDtoCollection is empty.");
                }
                return collection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при мапінгу колекції MessageData до MessageDto");
                throw;
            }
        }

        public override async Task<IEnumerable<MessageDto>> MapToDtoCollectionAsync(IEnumerable<MessageData> entities, int? userId = null)
        {
            try
            {
                if (entities == null)
                {
                    _logger.LogWarning("MapToDtoCollection received null entities collection.");
                    return Enumerable.Empty<MessageDto>();
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
                _logger.LogError(ex, "Помилка при мапінгу колекції MessageData до MessageDto");
                throw;
            }
        }

        [Obsolete("Цей метод не повинен використовуватися в цьому класі", true)]
        public override MessageData MapToEntity(MessageDto dto)
        {
            throw new NotImplementedException();
        }

        [Obsolete("Цей метод не повинен використовуватися в цьому класі", true)]
        public override void UpdateEntityFromDto(MessageDto dto, MessageData entity)
        {
            throw new NotImplementedException();
        }
    }
}
