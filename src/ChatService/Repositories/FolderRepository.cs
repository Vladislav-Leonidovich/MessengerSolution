using ChatService.Data;
using ChatService.Repositories.Interfaces;
using ChatServiceDTOs.Folders;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;

namespace ChatService.Repositories
{
    public class FolderRepository : IFolderRepository
    {
        private readonly ChatDbContext _context;
        private readonly ILogger<FolderRepository> _logger;

        public FolderRepository(ChatDbContext context, ILogger<FolderRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<FolderDto>> GetFoldersForUserAsync(int userId)
        {
            try
            {
                return await _context.Folders
                    .Where(f => f.UserId == userId)
                    .Select(f => new FolderDto
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Order = f.Order
                    })
                    .OrderBy(f => f.Order)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні папок для користувача {UserId}", userId);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }

        public async Task<FolderDto?> GetFolderByIdAsync(int folderId)
        {
            try
            {
                var folder = await _context.Folders
                    .FirstOrDefaultAsync(f => f.Id == folderId);

                if (folder == null)
                {
                    return null;
                }

                return new FolderDto
                {
                    Id = folder.Id,
                    Name = folder.Name,
                    Order = folder.Order
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні папки {FolderId}", folderId);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }

        public async Task<FolderDto> CreateFolderAsync(CreateFolderDto dto, int userId)
        {
            try
            {
                // Створюємо нову папку
                var folder = new ChatService.Models.Folder
                {
                    Name = dto.Name,
                    Order = dto.Order,
                    UserId = userId
                };

                _context.Folders.Add(folder);
                await _context.SaveChangesAsync();

                // Якщо в DTO є список чатів, додаємо їх до папки
                if (dto.ChatRoomIds != null && dto.ChatRoomIds.Any())
                {
                    var chatRooms = await _context.ChatRooms
                        .Where(cr => dto.ChatRoomIds.Contains(cr.Id))
                        .ToListAsync();

                    foreach (var chat in chatRooms)
                    {
                        chat.FolderId = folder.Id;
                    }

                    await _context.SaveChangesAsync();
                }

                return new FolderDto
                {
                    Id = folder.Id,
                    Name = folder.Name,
                    Order = folder.Order
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при створенні папки для користувача {UserId}", userId);
                throw new DatabaseException("Помилка при створенні папки", ex);
            }
        }

        public async Task<bool> UpdateFolderAsync(FolderDto folderDto, int userId)
        {
            try
            {
                var folder = await _context.Folders
                    .FirstOrDefaultAsync(f => f.Id == folderDto.Id && f.UserId == userId);

                if (folder == null)
                {
                    return false;
                }

                folder.Name = folderDto.Name;
                folder.Order = folderDto.Order;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при оновленні папки {FolderId}", folderDto.Id);
                throw new DatabaseException("Помилка при оновленні папки", ex);
            }
        }

        public async Task<bool> DeleteFolderAsync(int folderId)
        {
            try
            {
                var folder = await _context.Folders
                    .FirstOrDefaultAsync(f => f.Id == folderId);

                if (folder == null)
                {
                    return false;
                }

                // Видаляємо папку, а чати залишаються в системі, але без папки
                _context.Folders.Remove(folder);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при видаленні папки {FolderId}", folderId);
                throw new DatabaseException("Помилка при видаленні папки", ex);
            }
        }

        public async Task<bool> AssignChatToFolderAsync(int chatId, int folderId, bool isGroupChat)
        {
            try
            {
                // Знаходимо чат (приватний або груповий)
                var chat = await _context.ChatRooms
                    .FirstOrDefaultAsync(cr => cr.Id == chatId);

                if (chat == null)
                {
                    return false;
                }

                // Перевіряємо, чи існує папка
                var folderExists = await _context.Folders
                    .AnyAsync(f => f.Id == folderId);

                if (!folderExists)
                {
                    return false;
                }

                // Призначаємо папку
                chat.FolderId = folderId;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при призначенні чату {ChatId} до папки {FolderId}",
                    chatId, folderId);
                throw new DatabaseException("Помилка при призначенні чату до папки", ex);
            }
        }

        public async Task<bool> UnassignChatFromFolderAsync(int chatId, bool isGroupChat)
        {
            try
            {
                // Знаходимо чат (приватний або груповий)
                var chat = await _context.ChatRooms
                    .FirstOrDefaultAsync(cr => cr.Id == chatId);

                if (chat == null || chat.FolderId == null)
                {
                    return false;
                }

                // Видаляємо призначення папки
                chat.FolderId = null;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при видаленні чату {ChatId} з папки", chatId);
                throw new DatabaseException("Помилка при видаленні чату з папки", ex);
            }
        }
    }
}
