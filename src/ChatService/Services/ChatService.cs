using System.Security.Claims;
using ChatService.Data;
using ChatService.DTOs;
using ChatService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Services
{
    public class ChatService : IChatService
    {
        private readonly ChatDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ChatService(ChatDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // Метод для створення нового чату
        public async Task<ChatRoomDto> CreateChatRoomAsync(CreateChatRoomDto model)
        {
            // Створення нового об'єкту чату з назвою та поточною датою
            var chatRoom = new ChatRoom
            {
                Name = model.Name,
                CreatedAt = DateTime.UtcNow
            };

            // Додавання кожного користувача до чату
            foreach (var userId in model.UserIds)
            {
                chatRoom.UserChatRooms.Add(new UserChatRoom
                {
                    UserId = userId,
                    ChatRoom = chatRoom
                });
            }

            // Додавання об'єкту чату до бази даних
            _context.ChatRooms.Add(chatRoom);
            await _context.SaveChangesAsync();

            return new ChatRoomDto
            {
                Id = chatRoom.Id,
                Name = chatRoom.Name,
                CreatedAt = chatRoom.CreatedAt
            };
        }

        // Метод для отримання чатів, у яких бере участь користувач
        public async Task<IEnumerable<ChatRoomDto>> GetChatRoomsForUserAsync()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Enumerable.Empty<ChatRoomDto>();
            }

            // Виконуємо запит до БД, включаючи зв'язки з UserChatRooms
            return await _context.ChatRooms
                .Include(cr => cr.UserChatRooms)
                .Where(cr => cr.UserChatRooms.Any(uc => uc.UserId == userId))
                .Select(cr => new ChatRoomDto
                {
                    Id = cr.Id,
                    Name = cr.Name,
                    CreatedAt = cr.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<ChatRoomDto>> GetChatsForFolder(int folderId)
        {
            return await _context.ChatRooms
            .Where(cr => cr.FolderId == folderId)
            .Select(cr => new ChatRoomDto
            {
                Id = cr.Id,
                Name = cr.Name,
                CreatedAt = cr.CreatedAt
            })
            .ToListAsync();
        }

        public async Task<IEnumerable<ChatRoomDto>> GetChatsWithoutFolder()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Enumerable.Empty<ChatRoomDto>();
            }

            return await _context.ChatRooms
            .Include(cr => cr.UserChatRooms)
            .Where(cr => cr.UserChatRooms.Any(uc => uc.UserId == userId) && cr.FolderId == null)
            .Select(cr => new ChatRoomDto
            {
                Id = cr.Id,
                Name = cr.Name,
                CreatedAt = cr.CreatedAt
            })
            .ToListAsync();
        }
    }
}
