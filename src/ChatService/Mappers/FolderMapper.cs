using ChatService.Models;
using ChatServiceDTOs.Folders;

namespace ChatService.Mappers
{
    public class FolderMapper : BaseEntityMapper<Folder, FolderDto>
    {
        public override FolderDto MapToDto(Folder entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return new FolderDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Order = entity.Order
            };
        }

        public override Folder MapToEntity(FolderDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new Folder
            {
                Id = dto.Id,
                Name = dto.Name,
                Order = dto.Order
            };
        }

        public override void UpdateEntityFromDto(FolderDto dto, Folder entity)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            entity.Name = dto.Name;
            entity.Order = dto.Order;
        }
    }
}
