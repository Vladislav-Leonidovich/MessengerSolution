using ChatService.Mappers.Interfaces;
using ChatService.Models;
using ChatServiceDTOs.Chats;
using MessageServiceDTOs;

namespace ChatService.Mappers
{
    public class GroupChatRoomMapper : BaseEntityMapper<GroupChatRoom, GroupChatRoomDto>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IEntityMapper<GroupChatMember, GroupChatMemberDto> _memberMapper;
        private readonly ILogger<GroupChatRoomMapper> _logger;

        public GroupChatRoomMapper(
            IHttpClientFactory httpClientFactory,
            IEntityMapper<GroupChatMember, GroupChatMemberDto> memberMapper,
            ILogger<GroupChatRoomMapper> logger)
        {
            _httpClientFactory = httpClientFactory;
            _memberMapper = memberMapper;
            _logger = logger;
        }

        public override GroupChatRoomDto MapToDto(GroupChatRoom entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return new GroupChatRoomDto
            {
                Id = entity.Id,
                Name = entity.Name,
                CreatedAt = entity.CreatedAt,
                OwnerId = entity.OwnerId,
                ChatRoomType = entity.ChatRoomType,
                Members = entity.GroupChatMembers?.Select(_memberMapper.MapToDto) ?? new List<GroupChatMemberDto>(),
                LastMessagePreview = GetLastMessagePreviewAsync(entity.Id).GetAwaiter().GetResult()
            };
        }

        public async Task<GroupChatRoomDto> MapToDtoAsync(GroupChatRoom entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return new GroupChatRoomDto
            {
                Id = entity.Id,
                Name = entity.Name,
                CreatedAt = entity.CreatedAt,
                OwnerId = entity.OwnerId,
                ChatRoomType = entity.ChatRoomType,
                Members = entity.GroupChatMembers?.Select(_memberMapper.MapToDto) ?? new List<GroupChatMemberDto>(),
                LastMessagePreview = await GetLastMessagePreviewAsync(entity.Id)
            };
        }

        public override GroupChatRoom MapToEntity(GroupChatRoomDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

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

        public override void UpdateEntityFromDto(GroupChatRoomDto dto, GroupChatRoom entity)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            // Оновлюємо основні поля
            entity.Name = dto.Name;
            entity.FolderId = dto.FolderId;

            // Оновлення учасників - складніша логіка
            UpdateGroupMembers(dto.Members, entity);
        }

        private void UpdateGroupMembers(IEnumerable<GroupChatMemberDto> dtoMembers, GroupChatRoom entity)
        {
            var dtoMembersList = dtoMembers.ToList();
            var existingMembers = entity.GroupChatMembers.ToList();

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
