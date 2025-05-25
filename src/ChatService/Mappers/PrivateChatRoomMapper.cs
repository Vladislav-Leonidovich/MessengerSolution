using ChatService.Models;
using ChatServiceDTOs.Chats;
using MessageServiceDTOs;
using Shared.IdentityServiceDTOs;

namespace ChatService.Mappers
{
    public class PrivateChatRoomMapper : BaseEntityMapper<PrivateChatRoom, ChatRoomDto>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PrivateChatRoomMapper> _logger;

        public PrivateChatRoomMapper(
            IHttpClientFactory httpClientFactory,
            ILogger<PrivateChatRoomMapper> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public override ChatRoomDto MapToDto(PrivateChatRoom entity)
        {
            try
            {
                if (entity == null)
                {
                    _logger.LogError("PrivateChatRoom entity is null in MapToDto method.");
                    throw new ArgumentNullException(nameof(entity));
                }
                return new ChatRoomDto
                {
                    Id = entity.Id,
                    CreatedAt = entity.CreatedAt,
                    Name = GetChatNameAsync(entity).GetAwaiter().GetResult(),
                    ChatRoomType = entity.ChatRoomType,
                    ParticipantIds = entity.UserChatRooms?.Select(ucr => ucr.UserId) ?? new List<int>(),
                    LastMessagePreview = GetLastMessagePreviewAsync(entity.Id).GetAwaiter().GetResult()
                };
            } 
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при мапінгу PrivateChatRoom до ChatRoomDto");
                throw;
            }
        }

        // Асинхронна версія для кращої продуктивності
        public async Task<ChatRoomDto> MapToDtoAsync(PrivateChatRoom entity, int? currentUserId = null)
        {
            try
            {
                if (entity == null)
                {
                    _logger.LogError("PrivateChatRoom entity is null in MapToDtoAsync method.");
                    throw new ArgumentNullException(nameof(entity));
                }
                return new ChatRoomDto
                {
                    Id = entity.Id,
                    CreatedAt = entity.CreatedAt,
                    Name = await GetChatNameAsync(entity, currentUserId),
                    ChatRoomType = entity.ChatRoomType,
                    ParticipantIds = entity.UserChatRooms?.Select(ucr => ucr.UserId) ?? new List<int>(),
                    LastMessagePreview = await GetLastMessagePreviewAsync(entity.Id)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при мапінгу PrivateChatRoom до ChatRoomDto");
                throw;
            }
        }

        public override PrivateChatRoom MapToEntity(ChatRoomDto dto)
        {
            try
            {
                if (dto == null)
                {
                    _logger.LogError("ChatRoomDto is null in MapToEntity method.");
                    throw new ArgumentNullException(nameof(dto));
                }
                var entity = new PrivateChatRoom
                {
                    Id = dto.Id,
                    CreatedAt = dto.CreatedAt,
                    ChatRoomType = dto.ChatRoomType,
                    UserChatRooms = new List<UserChatRoom>()
                };

                // Додавання учасників
                foreach (var participantId in dto.ParticipantIds)
                {
                    entity.UserChatRooms.Add(new UserChatRoom
                    {
                        UserId = participantId,
                        PrivateChatRoom = entity
                    });
                }

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при мапінгу ChatRoomDto до PrivateChatRoom");
                throw;
            }
        }
        public IEnumerable<ChatRoomDto> MapToDtoCollection(IEnumerable<PrivateChatRoom>? entities)
        {
            try
            {
                if (entities == null)
                {
                    _logger.LogWarning("MapToDtoCollection received null entities collection.");
                    return Enumerable.Empty<ChatRoomDto>();
                }

                var collection = entities?.Select(e => MapToDto(e)) ?? Enumerable.Empty<ChatRoomDto>();
                if (!collection.Any() || collection == null)
                {
                    _logger.LogError("MapToDtoCollection is null.");
                    throw new InvalidOperationException("MapToDtoCollection returned null or empty collection.");
                }
                return collection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при мапінгу колекції PrivateChatRoom до ChatRoomDto");
                throw;
            }
        }
        public override void UpdateEntityFromDto(ChatRoomDto dto, PrivateChatRoom entity)
        {
            try
            {
                if (dto == null)
                {
                    _logger.LogError("ChatRoomDto is null in UpdateEntityFromDto method.");
                    throw new ArgumentNullException(nameof(dto));
                }
                if (entity == null)
                {
                    _logger.LogError("PrivateChatRoom entity is null in UpdateEntityFromDto method.");
                    throw new ArgumentNullException(nameof(entity));
                }
                entity.FolderId = dto.FolderId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при оновленні PrivateChatRoom з ChatRoomDto");
                throw;
            }
        }

        // Допоміжні методи
        private async Task<string> GetChatNameAsync(PrivateChatRoom chat, int? currentUserId = null)
        {
            try
            {
                // Для приватного чату назва - це ім'я співрозмовника
                var partnerId = chat.UserChatRooms?
                    .FirstOrDefault(ucr => !currentUserId.HasValue || ucr.UserId != currentUserId.Value)?.UserId;

                if (!partnerId.HasValue)
                {
                    return "Приватний чат";
                }

                var identityClient = _httpClientFactory.CreateClient("IdentityClient");
                var response = await identityClient.GetAsync($"api/users/search/id/{partnerId.Value}");

                if (response.IsSuccessStatusCode)
                {
                    var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
                    return userDto?.DisplayName ?? $"Користувач {partnerId.Value}";
                }

                return $"Користувач {partnerId.Value}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні імені користувача");
                return "Приватний чат";
            }
        }

        private async Task<MessageDto> GetLastMessagePreviewAsync(int chatRoomId)
        {
            try
            {
                var messageClient = _httpClientFactory.CreateClient("MessageClient");
                var response = await messageClient.GetAsync($"api/message/get-last-message/{chatRoomId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<MessageDto>() ?? new MessageDto();
                }

                return new MessageDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні останнього повідомлення для чату {ChatId}", chatRoomId);
                return new MessageDto();
            }
        }
    }
}
