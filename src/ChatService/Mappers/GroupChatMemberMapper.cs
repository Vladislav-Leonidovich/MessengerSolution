using ChatService.Models;
using ChatServiceDTOs.Chats;

namespace ChatService.Mappers
{
    public class GroupChatMemberMapper : BaseEntityMapper<GroupChatMember, GroupChatMemberDto>
    {
        public override GroupChatMemberDto MapToDto(GroupChatMember entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return new GroupChatMemberDto
            {
                UserId = entity.UserId,
                Role = entity.Role
            };
        }

        public override GroupChatMember MapToEntity(GroupChatMemberDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new GroupChatMember
            {
                UserId = dto.UserId,
                Role = dto.Role
            };
        }

        public override void UpdateEntityFromDto(GroupChatMemberDto dto, GroupChatMember entity)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            entity.Role = dto.Role;
        }
    }
}
