using ChatService.Mappers.Interfaces;
using ChatService.Models;
using ChatService.Repositories.Interfaces;
using ChatService.Services.Interfaces;
using Shared.DTOs.Chat;
using Shared.DTOs.Message;

namespace ChatService.Mappers
{
    public class GroupChatRoomMapper : BaseEntityMapper<GroupChatRoom, GroupChatRoomDto>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IChatRoomRepository _chatRoomRepository;
        private readonly IEntityMapper<GroupChatMember, GroupChatMemberDto> _memberMapper;
        private readonly ILogger<GroupChatRoomMapper> _logger;

        public GroupChatRoomMapper(
            IHttpClientFactory httpClientFactory,
            IChatRoomRepository chatRoomRepository,
            IEntityMapper<GroupChatMember, GroupChatMemberDto> memberMapper,
            ILogger<GroupChatRoomMapper> logger)
        {
            _httpClientFactory = httpClientFactory;
            _chatRoomRepository = chatRoomRepository;
            _memberMapper = memberMapper;
            _logger = logger;
        }

        public override GroupChatRoomDto MapToDto(GroupChatRoom entity, int? userId = null)
        {
            try
            {
                if (entity == null)
                {
                    _logger.LogError("GroupChatRoom entity is null in MapToDto method.");
                    throw new ArgumentNullException(nameof(entity));
                }

                return new GroupChatRoomDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    CreatedAt = entity.CreatedAt,
                    OwnerId = entity.OwnerId,
                    ChatRoomType = entity.ChatRoomType,
                    Members = entity.GroupChatMembers?.Select(e => _memberMapper.MapToDto(e, userId)) ?? new List<GroupChatMemberDto>(),
                    LastMessagePreview = _chatRoomRepository.GetLastMessagePreviewAsync(entity.Id).GetAwaiter().GetResult()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при мапінгу GroupChatRoom до GroupChatRoomDto");
                throw;
            }
        }

        public new async Task<GroupChatRoomDto> MapToDtoAsync(GroupChatRoom entity, int? userId = null)
        {
            try
            {
                if (entity == null)
                {
                    _logger.LogError("GroupChatRoom entity is null in MapToDtoAsync method.");
                    throw new ArgumentNullException(nameof(entity));
                }

                return new GroupChatRoomDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    CreatedAt = entity.CreatedAt,
                    OwnerId = entity.OwnerId,
                    ChatRoomType = entity.ChatRoomType,
                    Members = entity.GroupChatMembers?.Select(e => _memberMapper.MapToDto(e, userId)) ?? new List<GroupChatMemberDto>(),
                    LastMessagePreview = await _chatRoomRepository.GetLastMessagePreviewAsync(entity.Id)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping GroupChatRoom to GroupChatRoomDto");
                throw;
            }
        }

        public override GroupChatRoom MapToEntity(GroupChatRoomDto dto)
        {
            try
            {
                if (dto == null)
                {
                    _logger.LogError("GroupChatRoomDto is null in MapToEntity method.");
                    throw new ArgumentNullException(nameof(dto));
                }

                var entity = new GroupChatRoom
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    CreatedAt = dto.CreatedAt,
                    OwnerId = dto.OwnerId,
                    ChatRoomType = dto.ChatRoomType,
                    GroupChatMembers = new List<GroupChatMember>()
                };

                // Мапінг учасників
                foreach (var memberDto in dto.Members)
                {
                    var member = _memberMapper.MapToEntity(memberDto);
                    member.GroupChatRoom = entity;
                    entity.GroupChatMembers.Add(member);
                }

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping GroupChatRoomDto to GroupChatRoom");
                throw;
            }
        }
        public new IEnumerable<GroupChatRoomDto> MapToDtoCollection(IEnumerable<GroupChatRoom>? entities, int? userId = null)
        {
            try
            {
                if (entities == null)
                {
                    _logger.LogWarning("MapToDtoCollection received null entities collection.");
                    return Enumerable.Empty<GroupChatRoomDto>();
                }

                var collection = entities?.Select(e => MapToDto(e, userId)) ?? Enumerable.Empty<GroupChatRoomDto>();
                if (!collection.Any())
                {
                    _logger.LogInformation("MapToDtoCollection is empty.");
                }

                return collection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при мапінгу колекції GroupChatRoom до GroupChatRoomDto");
                throw;
            }
        }

        public new async Task<IEnumerable<GroupChatRoomDto>> MapToDtoCollectionAsync(IEnumerable<GroupChatRoom>? entities, int? userId = null)
        {
            try
            {
                if (entities == null)
                {
                    _logger.LogWarning("MapToDtoCollection received null entities collection.");
                    return Enumerable.Empty<GroupChatRoomDto>();
                }

                var tasks = entities.Select(e => MapToDtoAsync(e, userId));
                var results = await Task.WhenAll(tasks);

                if (!results.Any())
                {
                    _logger.LogInformation("MapToDtoCollectionAsync повернув порожню колекцію.");
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при мапінгу колекції GroupChatRoom до GroupChatRoomDto");
                throw;
            }
        }

        public override void UpdateEntityFromDto(GroupChatRoomDto dto, GroupChatRoom entity)
        {
            try
            {
                if (dto == null)
                {
                    _logger.LogError("GroupChatRoomDto is null in UpdateEntityFromDto method.");
                    throw new ArgumentNullException(nameof(dto));
                }
                if (entity == null)
                {
                    _logger.LogError("GroupChatRoom entity is null in UpdateEntityFromDto method.");
                    throw new ArgumentNullException(nameof(entity));
                }
                // Оновлюємо основні поля
                entity.Name = dto.Name;
                entity.FolderId = dto.FolderId;

                // Оновлення учасників - складніша логіка
                UpdateGroupMembers(dto.Members, entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating GroupChatRoom from GroupChatRoomDto");
                throw;
            }
        }

        private void UpdateGroupMembers(IEnumerable<GroupChatMemberDto> dtoMembers, GroupChatRoom entity)
        {
            try
            {
                if (dtoMembers == null)
                {
                    _logger.LogError("GroupChatMemberDto collection is null in UpdateGroupMembers method.");
                    throw new ArgumentNullException(nameof(dtoMembers));
                }
                if (entity == null)
                {
                    _logger.LogError("GroupChatRoom entity is null in UpdateGroupMembers method.");
                    throw new ArgumentNullException(nameof(entity));
                }

                var dtoMembersList = dtoMembers.ToList();
                var existingMembers = entity.GroupChatMembers.ToList();

                if (dtoMembersList == null || !dtoMembersList.Any())
                {
                    // Якщо в DTO немає учасників, видаляємо всіх існуючих
                    entity.GroupChatMembers.Clear();
                    _logger.LogInformation("No members in DTO, clearing all existing members from GroupChatRoom {ChatId}", entity.Id);
                    return;
                }
                // Видаляємо тих, кого немає в DTO
                var membersToRemove = existingMembers
                    .Where(em => !dtoMembersList.Any(dm => dm.UserId == em.UserId))
                    .ToList();

                foreach (var member in membersToRemove)
                {
                    entity.GroupChatMembers.Remove(member);
                }

                // Додаємо нових або оновлюємо існуючих
                foreach (var dtoMember in dtoMembersList)
                {
                    var existingMember = existingMembers
                        .FirstOrDefault(em => em.UserId == dtoMember.UserId);

                    if (existingMember != null)
                    {
                        // Оновлюємо роль
                        _memberMapper.UpdateEntityFromDto(dtoMember, existingMember);
                    }
                    else
                    {
                        // Додаємо нового учасника
                        var newMember = _memberMapper.MapToEntity(dtoMember);
                        newMember.GroupChatRoom = entity;
                        newMember.GroupChatRoomId = entity.Id;
                        entity.GroupChatMembers.Add(newMember);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при оновленні учасників групового чату");
                throw;
            }
        }
    }
}
