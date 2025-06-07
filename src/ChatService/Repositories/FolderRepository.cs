using ChatService.Data;
using ChatService.Models;
using ChatService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs.Folder;
using Shared.Exceptions;

namespace ChatService.Repositories
{
    public class FolderRepository : IFolderRepository
    {
        private readonly ChatDbContext _context;

        public FolderRepository(ChatDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<FolderDto>> GetFoldersForUserAsync(int userId)
        {
            try
            {
                var folders = await _context.Folders
                    .AsNoTracking()
                    .Where(f => f.UserId == userId)
                    .Select(f => new FolderDto
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Order = f.Order
                    })
                    .OrderBy(f => f.Order)
                    .ToListAsync();

                if (folders == null || !folders.Any())
                {
                    throw new EntityNotFoundException("Folders");
                }
                return folders;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Помилка при отриманні папок для користувача", ex);
            }
        }

        public async Task<FolderDto?> GetFolderByIdAsync(int folderId)
        {
            try
            {
                var folder = await _context.Folders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == folderId);

                if (folder == null)
                {
                    throw new EntityNotFoundException("Folder", folderId);
                }

                return new FolderDto
                {
                    Id = folder.Id,
                    Name = folder.Name,
                    Order = folder.Order
                };
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Помилка при отриманні папки", ex);
            }
        }

        public async Task<FolderDto> CreateFolderAsync(CreateFolderDto dto, int userId)
        {
            try
            {
                // Створюємо нову папку
                var folder = new Folder
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
                        .AsNoTracking()
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
                throw new DatabaseException("Помилка при створенні папки", ex);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Несподівана помилка при створенні папки", ex);
            }
        }

        public async Task<bool> UpdateFolderAsync(FolderDto folderDto, int userId)
        {
            try
            {
                var folder = await _context.Folders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == folderDto.Id && f.UserId == userId);

                if (folder == null)
                {
                    throw new EntityNotFoundException("Folder", folderDto.Id);
                }

                folder.Name = folderDto.Name;
                folder.Order = folderDto.Order;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                throw new DatabaseException("Помилка при оновленні папки", ex);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Несподівана помилка при оновленні папки", ex);
            }
        }

        public async Task<bool> DeleteFolderAsync(int folderId)
        {
            try
            {
                var folder = await _context.Folders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == folderId);

                if (folder == null)
                {
                    throw new EntityNotFoundException("Folder", folderId);
                }

                // Видаляємо папку, а чати залишаються в системі, але без папки
                _context.Folders.Remove(folder);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                throw new DatabaseException("Помилка при видаленні папки", ex);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Несподівана помилка при видаленні папки", ex);
            }
        }

        public async Task<bool> AssignChatToFolderAsync(int chatId, int folderId, bool isGroupChat)
        {
            try
            {
                // Знаходимо чат (приватний або груповий)
                var chat = await _context.ChatRooms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(cr => cr.Id == chatId);

                if (chat == null)
                {
                    throw new EntityNotFoundException("ChatRoom", chatId);
                }

                // Перевіряємо, чи існує папка
                var folderExists = await _context.Folders
                    .AsNoTracking()
                    .AnyAsync(f => f.Id == folderId);

                if (!folderExists)
                {
                    throw new EntityNotFoundException("Folder", folderId);
                }

                // Призначаємо папку
                chat.FolderId = folderId;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                throw new DatabaseException("Помилка при призначенні чату до папки", ex);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Несподівана помилка при призначенні чату до папки", ex);
            }
        }

        public async Task<bool> UnassignChatFromFolderAsync(int chatId, bool isGroupChat)
        {
            try
            {
                // Знаходимо чат (приватний або груповий)
                var chat = await _context.ChatRooms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(cr => cr.Id == chatId);

                if (chat == null || chat.FolderId == null)
                {
                    throw new EntityNotFoundException("ChatRoom", chatId);
                }

                // Видаляємо призначення папки
                chat.FolderId = null;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                throw new DatabaseException("Помилка при видаленні чату з папки", ex);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Несподівана помилка при видаленні чату з папки", ex);
            }
        }
        public async Task<bool> CanAccessFolderAsync(int userId, int folderId)
        {
            try
            {
                return await _context.Folders
                .AsNoTracking()
                .AnyAsync(f => f.Id == folderId && f.UserId == userId);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Помилка при перевірці доступу до папки", ex);
            }
        }

        public async Task<bool> IsFolderOwnerAsync(int userId, int folderId)
        {
            var folder = await _context.Folders
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == folderId && f.UserId == userId);

            return folder != null;
        }
    }
}
