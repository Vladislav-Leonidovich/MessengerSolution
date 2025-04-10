using ChatService.Data;
using ChatService.Repositories.Interfaces;
using ChatServiceDTOs.Chats;
using MessageServiceDTOs;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;
using Shared.IdentityServiceDTOs;
using ChatService.Models;
using ChatServiceModels.Chats;

namespace ChatService.Repositories
{
    public class ChatRoomRepository : IChatRoomRepository
    {
        private readonly ChatDbContext _context;
        private readonly ILogger<ChatRoomRepository> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ChatRoomRepository(
            ChatDbContext context,
            ILogger<ChatRoomRepository> logger,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ChatRoomDto?> GetPrivateChatByIdAsync(int chatRoomId)
        {
            try
            {
                var chat = await _context.PrivateChatRooms
                    .Include(pcr => pcr.UserChatRooms)
                    .FirstOrDefaultAsync(pcr => pcr.Id == chatRoomId);

                if (chat == null)
                {
                    _logger.LogWarning("Приватний чат з ID {ChatId} не знайдено", chatRoomId);
                    return null;
                }

                var lastMessagePreview = await GetLastMessagePreviewAsync(chatRoomId);

                return new ChatRoomDto
                {
                    Id = chat.Id,
                    CreatedAt = chat.CreatedAt,
                    Name = await GetChatNameAsync(chat),
                    ChatRoomType = chat.ChatRoomType,
                    LastMessagePreview = lastMessagePreview,
                    ParticipantIds = chat.UserChatRooms.Select(ucr => ucr.UserId).ToList()
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при отриманні чату {ChatId}", chatRoomId);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }

        public async Task<GroupChatRoomDto?> GetGroupChatByIdAsync(int chatRoomId)
        {
            try
            {
                var chat = await _context.GroupChatRooms
                    .Include(gcr => gcr.GroupChatMembers)
                    .FirstOrDefaultAsync(gcr => gcr.Id == chatRoomId);

                if (chat == null)
                {
                    _logger.LogWarning("Груповий чат з ID {ChatId} не знайдено", chatRoomId);
                    return null;
                }

                var lastMessagePreview = await GetLastMessagePreviewAsync(chatRoomId);

                return new GroupChatRoomDto
                {
                    Id = chat.Id,
                    Name = chat.Name,
                    CreatedAt = chat.CreatedAt,
                    OwnerId = chat.OwnerId,
                    ChatRoomType = chat.ChatRoomType,
                    LastMessagePreview = lastMessagePreview,
                    Members = chat.GroupChatMembers.Select(gcm => new GroupChatMemberDto
                    {
                        UserId = gcm.UserId,
                        Role = gcm.Role
                    }).ToList()
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при отриманні групового чату {ChatId}", chatRoomId);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }

        public async Task<IEnumerable<ChatRoomDto>> GetPrivateChatsForUserAsync(int userId)
        {
            try
            {
                var privateChats = await _context.PrivateChatRooms
                    .Include(pcr => pcr.UserChatRooms)
                    .Where(pcr => pcr.UserChatRooms.Any(ucr => ucr.UserId == userId))
                    .ToListAsync();

                var result = new List<ChatRoomDto>();

                foreach (var chat in privateChats)
                {
                    var lastMessagePreview = await GetLastMessagePreviewAsync(chat.Id);

                    result.Add(new ChatRoomDto
                    {
                        Id = chat.Id,
                        CreatedAt = chat.CreatedAt,
                        Name = await GetChatNameAsync(chat, userId),
                        LastMessagePreview = lastMessagePreview,
                        ChatRoomType = chat.ChatRoomType,
                        ParticipantIds = chat.UserChatRooms.Select(ucr => ucr.UserId).ToList()
                    });
                }

                return result;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при отриманні приватних чатів для користувача {UserId}", userId);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }

        public async Task<bool> UserBelongsToChatAsync(int userId, int chatRoomId)
        {
            try
            {
                // Перевіряємо в приватних чатах
                bool isPrivateChatMember = await _context.UserChatRooms
                    .AnyAsync(u => u.PrivateChatRoomId == chatRoomId && u.UserId == userId);

                if (isPrivateChatMember) return true;

                // Перевіряємо в групових чатах
                bool isGroupChatMember = await _context.GroupChatMembers
                    .AnyAsync(g => g.GroupChatRoomId == chatRoomId && g.UserId == userId);

                return isGroupChatMember;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Помилка при перевірці приналежності користувача {UserId} до чату {ChatId}",
                    userId, chatRoomId);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }

        public async Task<ChatRoomDto> CreatePrivateChatAsync(CreatePrivateChatRoomDto dto, int currentUserId)
        {
            try
            {
                // Перевіряємо, чи не існує вже чат між цими користувачами
                var existingChat = await _context.PrivateChatRooms
                    .Include(pcr => pcr.UserChatRooms)
                    .Where(pcr =>
                        pcr.UserChatRooms.Any(ucr => ucr.UserId == currentUserId) &&
                        pcr.UserChatRooms.Any(ucr => ucr.UserId == dto.UserId) &&
                        pcr.UserChatRooms.Count == 2) // Саме 2 користувача
                    .FirstOrDefaultAsync();

                if (existingChat != null)
                {
                    // Якщо чат вже існує, повертаємо його
                    return new ChatRoomDto
                    {
                        Id = existingChat.Id,
                        CreatedAt = existingChat.CreatedAt,
                        Name = await GetChatNameAsync(existingChat, currentUserId),
                        ChatRoomType = existingChat.ChatRoomType,
                        ParticipantIds = existingChat.UserChatRooms.Select(ucr => ucr.UserId).ToList(),
                        LastMessagePreview = await GetLastMessagePreviewAsync(existingChat.Id)
                    };
                }

                // Створюємо новий приватний чат
                var privateChatRoom = new ChatService.Models.PrivateChatRoom
                {
                    ChatRoomType = MessageServiceDTOs.ChatRoomType.privateChat,
                    CreatedAt = DateTime.UtcNow
                };

                // Додаємо учасників чату
                privateChatRoom.UserChatRooms.Add(new ChatService.Models.UserChatRoom
                {
                    UserId = currentUserId
                });

                privateChatRoom.UserChatRooms.Add(new ChatService.Models.UserChatRoom
                {
                    UserId = dto.UserId
                });

                // Зберігаємо в базу даних
                _context.PrivateChatRooms.Add(privateChatRoom);
                await _context.SaveChangesAsync();

                // Формуємо відповідь
                return new ChatRoomDto
                {
                    Id = privateChatRoom.Id,
                    CreatedAt = privateChatRoom.CreatedAt,
                    Name = await GetChatNameAsync(privateChatRoom, currentUserId),
                    ChatRoomType = privateChatRoom.ChatRoomType,
                    ParticipantIds = privateChatRoom.UserChatRooms.Select(ucr => ucr.UserId).ToList(),
                    LastMessagePreview = new MessageDto() // Порожнє повідомлення для нового чату
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при створенні приватного чату");
                throw new DatabaseException("Помилка при створенні приватного чату", ex);
            }
        }

        public async Task<bool> DeletePrivateChatAsync(int chatRoomId)
        {
            try
            {
                var chat = await _context.PrivateChatRooms
                    .FirstOrDefaultAsync(c => c.Id == chatRoomId);

                if (chat == null)
                {
                    _logger.LogWarning("Спроба видалити неіснуючий приватний чат {ChatId}", chatRoomId);
                    return false;
                }

                _context.PrivateChatRooms.Remove(chat);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при видаленні приватного чату {ChatId}", chatRoomId);
                throw new DatabaseException("Помилка при видаленні приватного чату", ex);
            }
        }

        public async Task<IEnumerable<GroupChatRoomDto>> GetGroupChatsForUserAsync(int userId)
        {
            try
            {
                var groupChats = _context.GroupChatRooms
                    .Include(gcr => gcr.GroupChatMembers)
                    .Where(gcr => gcr.GroupChatMembers.Any(gcm => gcm.UserId == userId))
                    .ToList();
                var result = new List<GroupChatRoomDto>();
                foreach (var chat in groupChats)
                {
                    var lastMessagePreview = await GetLastMessagePreviewAsync(chat.Id);
                    result.Add(new GroupChatRoomDto
                    {
                        Id = chat.Id,
                        Name = chat.Name,
                        CreatedAt = chat.CreatedAt,
                        OwnerId = chat.OwnerId,
                        ChatRoomType = chat.ChatRoomType,
                        LastMessagePreview = lastMessagePreview,
                        Members = chat.GroupChatMembers.Select(gcm => new GroupChatMemberDto
                        {
                            UserId = gcm.UserId,
                            Role = gcm.Role
                        }).ToList()
                    });
                }
                return result;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при отриманні групових чатів для користувача {UserId}", userId);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }

        public async Task<IEnumerable<ChatRoomDto>> GetPrivateChatsForFolderAsync(int folderId)
        {
            try
            {
                var privateChats = _context.PrivateChatRooms
                    .Include(pcr => pcr.UserChatRooms)
                    .Where(pcr => pcr.FolderId == folderId)
                    .ToList();
                var result = new List<ChatRoomDto>();
                foreach (var chat in privateChats)
                {
                    var lastMessagePreview = await GetLastMessagePreviewAsync(chat.Id);
                    result.Add(new ChatRoomDto
                    {
                        Id = chat.Id,
                        Name = await GetChatNameAsync(chat),
                        CreatedAt = chat.CreatedAt,
                        ChatRoomType = chat.ChatRoomType,
                        LastMessagePreview = lastMessagePreview,
                        ParticipantIds = chat.UserChatRooms.Select(ucr => ucr.UserId).ToList()
                    });
                }
                return result;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при отриманні приватних чатів для папки {FolderId}", folderId);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }

        public async Task<IEnumerable<GroupChatRoomDto>> GetGroupChatsForFolderAsync(int folderId)
        {
            try
            {
                var groupChats = _context.GroupChatRooms
                    .Include(gcr => gcr.GroupChatMembers)
                    .Where(gcr => gcr.FolderId == folderId)
                    .ToList();
                var result = new List<GroupChatRoomDto>();
                foreach (var chat in groupChats)
                {
                    var lastMessagePreview = await GetLastMessagePreviewAsync(chat.Id);
                    result.Add(new GroupChatRoomDto
                    {
                        Id = chat.Id,
                        Name = chat.Name,
                        CreatedAt = chat.CreatedAt,
                        OwnerId = chat.OwnerId,
                        ChatRoomType = chat.ChatRoomType,
                        LastMessagePreview = lastMessagePreview,
                        Members = chat.GroupChatMembers.Select(gcm => new GroupChatMemberDto
                        {
                            UserId = gcm.UserId,
                            Role = gcm.Role
                        }).ToList()
                    });
                }
                return result;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при отриманні групових чатів для папки {FolderId}", folderId);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }

        public async Task<IEnumerable<ChatRoomDto>> GetPrivateChatsWithoutFolderAsync(int userId)
        {
            try
            {
                var privateChats = _context.PrivateChatRooms
                    .Include(pcr => pcr.UserChatRooms)
                    .Where(pcr => pcr.UserChatRooms.Any(ucr => ucr.UserId == userId) && pcr.FolderId == null)
                    .ToList();
                var result = new List<ChatRoomDto>();
                foreach (var chat in privateChats)
                {
                    var lastMessagePreview = await GetLastMessagePreviewAsync(chat.Id);
                    result.Add(new ChatRoomDto
                    {
                        Id = chat.Id,
                        Name = await GetChatNameAsync(chat),
                        CreatedAt = chat.CreatedAt,
                        ChatRoomType = chat.ChatRoomType,
                        LastMessagePreview = lastMessagePreview,
                        ParticipantIds = chat.UserChatRooms.Select(ucr => ucr.UserId).ToList()
                    });
                }
                return result;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при отриманні приватних чатів без папки для користувача {UserId}", userId);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }

        public async Task<IEnumerable<GroupChatRoomDto>> GetGroupChatsWithoutFolderAsync(int userId)
        {
            try
            {
                var groupChats = _context.GroupChatRooms
                    .Include(gcr => gcr.GroupChatMembers)
                    .Where(gcr => gcr.GroupChatMembers.Any(gcm => gcm.UserId == userId) && gcr.FolderId == null)
                    .ToList();
                var result = new List<GroupChatRoomDto>();
                foreach (var chat in groupChats)
                {
                    var lastMessagePreview = await GetLastMessagePreviewAsync(chat.Id);
                    result.Add(new GroupChatRoomDto
                    {
                        Id = chat.Id,
                        Name = chat.Name,
                        CreatedAt = chat.CreatedAt,
                        OwnerId = chat.OwnerId,
                        ChatRoomType = chat.ChatRoomType,
                        LastMessagePreview = lastMessagePreview,
                        Members = chat.GroupChatMembers.Select(gcm => new GroupChatMemberDto
                        {
                            UserId = gcm.UserId,
                            Role = gcm.Role
                        }).ToList()
                    });
                }
                return result;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при отриманні групових чатів без папки для користувача {UserId}", userId);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }

        public async Task<GroupChatRoomDto> CreateGroupChatAsync(CreateGroupChatRoomDto dto, int currentUserId)
        {
            try
            {
                var allMemberIds = new HashSet<int>(dto.MemberIds);
                allMemberIds.Add(currentUserId); // Додаємо власника, якщо його ще немає у списку

                // Перевіряємо, чи не існує вже чат між цими користувачами
                var existingChat = await _context.GroupChatRooms
                    .Include(gcr => gcr.GroupChatMembers)
                    .Where(gcr =>
                        // Перевіряємо, що кількість учасників у чаті дорівнює загальній кількості (з власником)
                        gcr.GroupChatMembers.Count == allMemberIds.Count &&
                        gcr.OwnerId == currentUserId &&
                        // Перевіряємо, що всі наші користувачі є учасниками чату
                        !allMemberIds.Except(gcr.GroupChatMembers.Select(gcm => gcm.UserId)).Any() &&
                        // Перевіряємо, що в чаті немає інших користувачів, крім наших
                        !gcr.GroupChatMembers.Select(gcm => gcm.UserId).Except(allMemberIds).Any())
                    .FirstOrDefaultAsync();

                if (existingChat != null)
                {
                    // Якщо чат вже існує, повертаємо його
                    return new GroupChatRoomDto
                    {
                        Id = existingChat.Id,
                        CreatedAt = existingChat.CreatedAt,
                        Name = existingChat.Name,
                        ChatRoomType = existingChat.ChatRoomType,
                        LastMessagePreview = await GetLastMessagePreviewAsync(existingChat.Id),
                        OwnerId = existingChat.OwnerId,
                        Members = existingChat.GroupChatMembers.Select(gcm => new GroupChatMemberDto
                        {
                            UserId = gcm.UserId,
                            Role = gcm.Role
                        }).ToList()
                    };
                }

                // Створюємо новий груповий чат
                var groupChatRoom = new GroupChatRoom
                {
                    ChatRoomType = ChatRoomType.groupChat,
                    CreatedAt = DateTime.UtcNow,
                    OwnerId = currentUserId,
                    Name = dto.Name,
                    GroupChatMembers = allMemberIds.Select(userId => new GroupChatMember
                    {
                        UserId = userId,
                        Role = userId == currentUserId ? GroupRole.Owner : GroupRole.Member
                    }).ToList()

                };

                // Зберігаємо в базу даних
                _context.GroupChatRooms.Add(groupChatRoom);
                await _context.SaveChangesAsync();

                // Формуємо відповідь
                return new GroupChatRoomDto
                {
                    Id = groupChatRoom.Id,
                    CreatedAt = groupChatRoom.CreatedAt,
                    Name = groupChatRoom.Name,
                    ChatRoomType = groupChatRoom.ChatRoomType,
                    Members = groupChatRoom.GroupChatMembers.Select(gcm => new GroupChatMemberDto
                    {
                        UserId = gcm.UserId,
                        Role = gcm.Role
                    }).ToList(),
                    OwnerId = groupChatRoom.OwnerId,
                    LastMessagePreview = new MessageDto() // Порожнє повідомлення для нового чату
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при створенні групового чату");
                throw new DatabaseException("Помилка при створенні групового чату", ex);
            }
        }

        public async Task<bool> DeleteGroupChatAsync(int chatRoomId)
        {
            try
            {
                var chat = await _context.GroupChatRooms
                    .FirstOrDefaultAsync(c => c.Id == chatRoomId);

                if (chat == null)
                {
                    _logger.LogWarning("Спроба видалити неіснуючий приватний чат {ChatId}", chatRoomId);
                    return false;
                }

                _context.GroupChatRooms.Remove(chat);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при видаленні групового чату {ChatId}", chatRoomId);
                throw new DatabaseException("Помилка при видаленні групового чату", ex);
            }
        }

        // Вспоміжні методи для отримання даних
        private async Task<string> GetChatNameAsync(PrivateChatRoom chat, int currentUserId = 0)
        {
            // Для приватного чату назвою є відображуване ім'я співрозмовника
            int? partnerId = chat.UserChatRooms
                .FirstOrDefault(ucr => ucr.UserId != currentUserId)?.UserId;

            if (!partnerId.HasValue)
            {
                return "Приватний чат";
            }

            try
            {
                // Запит до сервісу ідентифікації для отримання імені користувача
                var identityClient = _httpClientFactory.CreateClient("IdentityClient");
                var userDto = await identityClient.GetFromJsonAsync<UserDto>($"api/users/search/id/{partnerId.Value}");
                return userDto?.DisplayName ?? $"Користувач {partnerId.Value}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні імені користувача {UserId}", partnerId.Value);
                return $"Користувач {partnerId.Value}";
            }
        }

        private async Task<MessageDto> GetLastMessagePreviewAsync(int chatRoomId)
        {
            try
            {
                // Запит до сервісу повідомлень для отримання останнього повідомлення
                var messageClient = _httpClientFactory.CreateClient("MessageClient");
                var response = await messageClient.GetFromJsonAsync<MessageDto>($"api/message/get-last-message/{chatRoomId}");
                return response ?? new MessageDto(); // Порожнє повідомлення, якщо нічого не знайдено
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні останнього повідомлення для чату {ChatId}", chatRoomId);
                return new MessageDto();
            }
        }
    }
}
