using ChatService.DTOs;
using ChatService.Models;
using ChatService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ChatService.Services
{
    public class FolderService : IFolderService
    {
        private readonly ChatDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FolderService(ChatDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // Метод для створення нової папки
        public async Task<FolderDto> CreateFolderAsync(CreateFolderDto model)
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return new FolderDto();
            }

            // Створюємо нову папку
            var folder = new Folder
            {
                Name = model.Name,
                Order = model.Order,
                UserId = userId
            };

            _context.Folders.Add(folder);
            await _context.SaveChangesAsync(); // Зберігаємо, щоб отримати folder.Id

            // Якщо в DTO вказані ідентифікатори чатів, призначаємо їх цій папці
            if (model.ChatRoomIds != null && model.ChatRoomIds.Any())
            {
                var chats = await _context.ChatRooms
                    .Where(cr => model.ChatRoomIds.Contains(cr.Id))
                    .ToListAsync();

                foreach (var chat in chats)
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

        public async Task<IEnumerable<FolderDto>> GetFoldersAsync()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Enumerable.Empty<FolderDto>();
            }

            return await _context.Folders
                .Where(f => f.UserId == userId)
                .Select(f => new FolderDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Order = f.Order
                })
                .ToListAsync();
        }

        public async Task<bool> UpdateFolderAsync(FolderDto folderDto)
        {
            var folder = await _context.Folders.FindAsync(folderDto.Id);
            if (folder == null)
                return false;

            folder.Name = folderDto.Name;
            folder.Order = folderDto.Order;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteFolderAsync(int folderId)
        {
            var folder = await _context.Folders.FindAsync(folderId);
            if (folder == null)
                return false;

            _context.Folders.Remove(folder);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignChatToFolderAsync(int chatId, int folderId)
        {
            var chat = await _context.ChatRooms.FindAsync(chatId);
            var folder = await _context.Folders.FindAsync(folderId);
            if (chat == null || folder == null)
                return false;

            chat.FolderId = folderId;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnassignChatFromFolderAsync(int chatId)
        {
            var chat = await _context.ChatRooms.FindAsync(chatId);
            if (chat == null || chat.FolderId == null)
                return false;

            chat.FolderId = null;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
