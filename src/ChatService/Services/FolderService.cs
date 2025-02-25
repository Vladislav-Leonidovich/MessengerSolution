using ChatServiceDTOs.Folders;
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
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

            // Створюємо нову папку
            var folder = new Folder
            {
                Name = model.Name,
                Order = model.Order,
                UserId = currentUserId
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
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

            return await _context.Folders
                .Where(f => f.UserId == currentUserId)
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
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

            var folder = await _context.Folders.FirstOrDefaultAsync(f => f.UserId == currentUserId && f.Id == folderDto.Id);

            if (folder == null)
                return false;

            folder.Name = folderDto.Name;
            folder.Order = folderDto.Order;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteFolderAsync(int folderId)
        {
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

            var folder = await _context.Folders.FirstOrDefaultAsync(f => f.UserId == currentUserId && f.Id == folderId);
            if (folder == null)
                return false;

            _context.Folders.Remove(folder);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignPrivateChatToFolderAsync(int chatId, int folderId)
        {
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

            var chat = await _context.PrivateChatRooms.FirstOrDefaultAsync(pcr => pcr.Id == chatId && pcr.UserChatRooms.Any(ucr => ucr.UserId == currentUserId));
            var folder = await _context.Folders.FirstOrDefaultAsync(f => f.Id == folderId && f.UserId == currentUserId);

            if (chat == null || folder == null)
                return false;

            chat.FolderId = folderId;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnassignPrivateChatFromFolderAsync(int chatId)
        {
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

            var chat = await _context.PrivateChatRooms.FirstOrDefaultAsync(pcr => pcr.Id == chatId && pcr.UserChatRooms.Any(ucr => ucr.UserId == currentUserId));
            if (chat == null || chat.FolderId == null)
                return false;

            chat.FolderId = null;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignGroupChatToFolderAsync(int chatId, int folderId)
        {
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

            var chat = await _context.GroupChatRooms.FirstOrDefaultAsync(gcr => gcr.Id == chatId && gcr.GroupChatMembers.Any(gcm => gcm.UserId == currentUserId));
            var folder = await _context.Folders.FirstOrDefaultAsync(f => f.Id == folderId && f.UserId == currentUserId);

            if (chat == null || folder == null)
                return false;

            chat.FolderId = folderId;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnassignGroupChatFromFolderAsync(int chatId)
        {
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

            var chat = await _context.GroupChatRooms.FirstOrDefaultAsync(gcr => gcr.Id == chatId && gcr.GroupChatMembers.Any(gcm => gcm.UserId == currentUserId));
            if (chat == null || chat.FolderId == null)
                return false;

            chat.FolderId = null;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
