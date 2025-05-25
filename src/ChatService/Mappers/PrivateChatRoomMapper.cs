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
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

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

        // Асинхронна версія для кращої продуктивності
        public async Task<ChatRoomDto> MapToDtoAsync(PrivateChatRoom entity, int? currentUserId = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

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

        public override PrivateChatRoom MapToEntity(ChatRoomDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

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

        public override void UpdateEntityFromDto(ChatRoomDto dto, PrivateChatRoom entity)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            entity.FolderId = dto.FolderId;
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
