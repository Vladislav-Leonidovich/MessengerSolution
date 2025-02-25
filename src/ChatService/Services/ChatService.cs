using System.Linq;
using System.Security.Claims;
using ChatService.Data;
using ChatServiceDTOs.Chats;
using ChatServiceModels.Chats;
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

        // Метод для створення групового чату
        public async Task<GroupChatRoomDto> CreateGroupChatRoomAsync(CreateGroupChatRoomDto model)
        {
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

            // Створюємо новий груповий чат із встановленою назвою і власником
            var groupChat = new GroupChatRoom
            {
                Name = model.Name,
                OwnerId = currentUserId,
                CreatedAt = DateTime.UtcNow
            };

            // Додаємо власника до групи з роллю Owner
            groupChat.GroupChatMembers.Add(new GroupChatMember
            {
                UserId = currentUserId,
                Role = GroupRole.Owner
            });

            // Додаємо інших учасників (як Member за замовчуванням)
            foreach (var memberId in model.MemberIds)
            {
                if (memberId == currentUserId) continue; // не дублювати власника
                groupChat.GroupChatMembers.Add(new GroupChatMember
                {
                    UserId = memberId,
                    Role = GroupRole.Member
                });
            }

            _context.GroupChatRooms.Add(groupChat);
            await _context.SaveChangesAsync();

            // Формуємо DTO для відповіді
            var dto = new GroupChatRoomDto
            {
                Id = groupChat.Id,
                Name = groupChat.Name,
                CreatedAt = groupChat.CreatedAt,
                OwnerId = groupChat.OwnerId,
                Members = groupChat.GroupChatMembers.Select(gm => new GroupChatMemberDto
                {
                    UserId = gm.UserId,
                    Role = gm.Role
                }).ToList()
            };

            return dto;
        }

        // Метод для створення нового приватного чату
        public async Task<ChatRoomDto> CreatePrivateChatRoomAsync(CreatePrivateChatRoomDto model)
        {
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

            // Створення нового об'єкту чату з назвою та поточною датою
            var privateChatRoom = new PrivateChatRoom
            {
                CreatedAt = DateTime.UtcNow
            };

            privateChatRoom.UserChatRooms.Add(new UserChatRoom
            {
                UserId = model.UserId,
                PrivateChatRoom = privateChatRoom
            });

            privateChatRoom.UserChatRooms.Add(new UserChatRoom
            {
                UserId = currentUserId,
                PrivateChatRoom = privateChatRoom
            });

            // Додавання об'єкту чату до бази даних
            _context.PrivateChatRooms.Add(privateChatRoom);
            await _context.SaveChangesAsync();

            var dto = new ChatRoomDto
            {
                Id = privateChatRoom.Id,
                CreatedAt = privateChatRoom.CreatedAt
            };

            return dto;
        }

        // Метод для отримання всіх приватних чатів, у яких бере участь користувач
        public async Task<IEnumerable<ChatRoomDto>> GetPrivateChatRoomsForUserAsync()
        {
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

            // Отримуємо приватні чати, де поточний користувач бере участь
            var privateChats = await _context.PrivateChatRooms
                .Include(pcr => pcr.UserChatRooms)
                .Where(pcr => pcr.UserChatRooms.Any(puc => puc.UserId == currentUserId))
                .Select(pcr => new ChatRoomDto
                {
                    Id = pcr.Id,
                    CreatedAt = pcr.CreatedAt
                })
                .ToListAsync();

            return privateChats;
        }

        // Метод для отримання всіх приватних чатів, у яких бере участь користувач
        public async Task<IEnumerable<GroupChatRoomDto>> GetGroupChatRoomsForUserAsync()
        {
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

            // Отримуємо групові чати, де поточний користувач є учасником
            var groupChats = await _context.GroupChatRooms
                .Include(gc => gc.GroupChatMembers)
                .Where(gc => gc.GroupChatMembers.Any(gm => gm.UserId == currentUserId))
                .Select(gc => new GroupChatRoomDto
                {
                    Id = gc.Id,
                    Name = gc.Name, // Назва групового чату, встановлена власником
                    CreatedAt = gc.CreatedAt
                })
                .ToListAsync();

            return groupChats;
        }

        public async Task<IEnumerable<ChatRoomDto>> GetPrivateChatsForFolderAsync(int folderId)
        {
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

            return await _context.PrivateChatRooms
            .Where(cr => cr.UserChatRooms.Any(uc => uc.UserId == currentUserId) && cr.FolderId == folderId)
            .Select(cr => new ChatRoomDto
            {
                Id = cr.Id,
                CreatedAt = cr.CreatedAt
            })
            .ToListAsync();
        }

        public async Task<IEnumerable<GroupChatRoomDto>> GetGroupChatsForFolderAsync(int folderId)
        {
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

            return await _context.GroupChatRooms
            .Where(gr => gr.GroupChatMembers.Any(gc => gc.UserId == currentUserId) && gr.FolderId == folderId)
            .Select(gr => new GroupChatRoomDto
            {
                Id = gr.Id,
                Name = gr.Name,
                CreatedAt = gr.CreatedAt,
                OwnerId = gr.OwnerId,
                Members = gr.GroupChatMembers.Select(gm => new GroupChatMemberDto
                {
                    UserId = gm.UserId,
                    Role = gm.Role
                }).ToList()
            })
            .ToListAsync();
        }

        public async Task<IEnumerable<ChatRoomDto>> GetPrivateChatsWithoutFolderAsync()
        {
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

            return await _context.PrivateChatRooms
            .Include(cr => cr.UserChatRooms)
            .Where(cr => cr.UserChatRooms.Any(uc => uc.UserId == currentUserId) && cr.FolderId == null)
            .Select(cr => new ChatRoomDto
            {
                Id = cr.Id,
                CreatedAt = cr.CreatedAt
            })
            .ToListAsync();
        }

        public async Task<IEnumerable<GroupChatRoomDto>> GetGroupChatsWithoutFolderAsync()
        {
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

            return await _context.GroupChatRooms
            .Where(gr => gr.GroupChatMembers.Any(gc => gc.UserId == currentUserId) && gr.FolderId == null)
            .Select(gr => new GroupChatRoomDto
            {
                Id = gr.Id,
                Name = gr.Name,
                CreatedAt = gr.CreatedAt,
                OwnerId = gr.OwnerId,
                Members = gr.GroupChatMembers.Select(gm => new GroupChatMemberDto
                {
                    UserId = gm.UserId,
                    Role = gm.Role
                }).ToList()
            })
            .ToListAsync();
        }
    }
}
