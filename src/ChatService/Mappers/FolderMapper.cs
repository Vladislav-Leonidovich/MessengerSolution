using ChatService.Models;
using Shared.DTOs.Folder;

namespace ChatService.Mappers
{
    public class FolderMapper : BaseEntityMapper<Folder, FolderDto>
    {
        private readonly ILogger<FolderMapper> _logger;

        public FolderMapper(ILogger<FolderMapper> logger)
        {
            _logger = logger;
        }
        public override FolderDto MapToDto(Folder entity, int? userId = null)
        {
            try
            {
                if (entity == null)
                {
                    _logger.LogError("Folder entity is null in MapToDto method.");
                    throw new ArgumentNullException(nameof(entity));
                }

                return new FolderDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Order = entity.Order
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping Folder to FolderDto");
                throw;
            }
        }

        public override Folder MapToEntity(FolderDto dto)
        {
            try
            {
                if (dto == null)
                {
                    _logger.LogError("FolderDto is null in MapToEntity method.");
                    throw new ArgumentNullException(nameof(dto));
                }

                return new Folder
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Order = dto.Order
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping FolderDto to Folder");
                throw;
            }
        }

        public override void UpdateEntityFromDto(FolderDto dto, Folder entity)
        {
            try
            {
                if (dto == null)
                {
                    _logger.LogError("FolderDto is null in UpdateEntityFromDto method.");
                    throw new ArgumentNullException(nameof(dto));
                }
                if (entity == null)
                {
                    _logger.LogError("Folder entity is null in UpdateEntityFromDto method.");
                    throw new ArgumentNullException(nameof(entity));
                }

                entity.Name = dto.Name;
                entity.Order = dto.Order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Folder entity from FolderDto");
                throw;
            }
        }
    }
}
