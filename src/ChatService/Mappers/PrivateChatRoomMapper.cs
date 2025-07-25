﻿using System.Security.Claims;
using ChatService.Models;
using ChatService.Repositories;
using ChatService.Repositories.Interfaces;
using ChatService.Services.Interfaces;
using Shared.DTOs.Chat;
using Shared.DTOs.Identity;
using Shared.DTOs.Message;

namespace ChatService.Mappers
{
    public class PrivateChatRoomMapper : BaseEntityMapper<PrivateChatRoom, ChatRoomDto>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IChatRoomRepository _chatRoomRepository;
        private readonly ILogger<PrivateChatRoomMapper> _logger;

        public PrivateChatRoomMapper(
            IHttpContextAccessor httpContextAccessor,
            IChatRoomRepository chatRepository,
            ILogger<PrivateChatRoomMapper> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _chatRoomRepository = chatRepository;
            _logger = logger;
        }

        public override ChatRoomDto MapToDto(PrivateChatRoom entity, int? userId)
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
                    Name = _chatRoomRepository.GetChatNameAsync(entity.Id, GetCurrentUserId()).GetAwaiter().GetResult(),
                    ChatRoomType = entity.ChatRoomType,
                    ParticipantIds = entity.UserChatRooms?.Select(ucr => ucr.UserId) ?? new List<int>(),
                    LastMessagePreview = _chatRoomRepository.GetLastMessagePreviewAsync(entity.Id).GetAwaiter().GetResult()
                };
            } 
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при мапінгу PrivateChatRoom до ChatRoomDto");
                throw;
            }
        }

        // Асинхронна версія для кращої продуктивності
        public new async Task<ChatRoomDto> MapToDtoAsync(PrivateChatRoom entity, int? userId)
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
                    Name = await _chatRoomRepository.GetChatNameAsync(entity.Id, GetCurrentUserId()),
                    ChatRoomType = entity.ChatRoomType,
                    ParticipantIds = entity.UserChatRooms?.Select(ucr => ucr.UserId) ?? new List<int>(),
                    LastMessagePreview = await _chatRoomRepository.GetLastMessagePreviewAsync(entity.Id)
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
        public new IEnumerable<ChatRoomDto> MapToDtoCollection(IEnumerable<PrivateChatRoom>? entities, int? userId)
        {
            try
            {
                if (entities == null)
                {
                    _logger.LogWarning("MapToDtoCollection received null entities collection.");
                    return Enumerable.Empty<ChatRoomDto>();
                }

                var collection = entities?.Select(e => MapToDto(e, userId)) ?? Enumerable.Empty<ChatRoomDto>();
                if (!collection.Any())
                {
                    _logger.LogError("MapToDtoCollection is empty.");
                }
                return collection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при мапінгу колекції PrivateChatRoom до ChatRoomDto");
                throw;
            }
        }
        public new async Task<IEnumerable<ChatRoomDto>> MapToDtoCollectionAsync(IEnumerable<PrivateChatRoom>? entities, int? userId)
        {
            try
            {
                if (entities == null)
                {
                    _logger.LogWarning("MapToDtoCollection received null entities collection.");
                    return Enumerable.Empty<ChatRoomDto>();
                }

                var tasks = entities.Select(e => MapToDtoAsync(e, userId));
                var results = await Task.WhenAll(tasks);

                if (!results.Any())
                {
                    _logger.LogError("MapToDtoCollection is empty.");
                }
                return results;
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

        private int GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }
            return userId;
        }
    }
}
