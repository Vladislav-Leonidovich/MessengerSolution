using ChatService.Models;
using Shared.DTOs.Chat;

namespace ChatService.Mappers
{
    public class GroupChatMemberMapper : BaseEntityMapper<GroupChatMember, GroupChatMemberDto>
    {
        private readonly ILogger<GroupChatMemberMapper> _logger;

        public GroupChatMemberMapper(ILogger<GroupChatMemberMapper> logger)
        {
            _logger = logger;
        }
        public override GroupChatMemberDto MapToDto(GroupChatMember entity, int? userId = null)
        {
            try
            {
                if (entity == null)
                {
                    _logger.LogError("GroupChatMember entity is null in MapToDto method.");
                    throw new ArgumentNullException(nameof(entity));
                }

                return new GroupChatMemberDto
                {
                    UserId = entity.UserId,
                    Role = entity.Role
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping GroupChatMember to GroupChatMemberDto");
                throw;
            }
        }

        public override GroupChatMember MapToEntity(GroupChatMemberDto dto)
        {
            try
            {
                if (dto == null)
                {
                    _logger.LogError("GroupChatMemberDto is null in MapToEntity method.");
                    throw new ArgumentNullException(nameof(dto));
                }
                return new GroupChatMember
                {
                    UserId = dto.UserId,
                    Role = dto.Role
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping GroupChatMemberDto to GroupChatMember");
                throw;
            }
        }

        public override void UpdateEntityFromDto(GroupChatMemberDto dto, GroupChatMember entity)
        {
            try
            {
                if (dto == null)
                {
                    _logger.LogError("GroupChatMemberDto is null in UpdateEntityFromDto method.");
                    throw new ArgumentNullException(nameof(dto));
                }
                if (entity == null)
                {
                    _logger.LogError("GroupChatMember entity is null in UpdateEntityFromDto method.");
                    throw new ArgumentNullException(nameof(entity));
                }
                entity.Role = dto.Role;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating GroupChatMember from GroupChatMemberDto");
                throw;
            }
        }
    }
}
