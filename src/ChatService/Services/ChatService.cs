using System.Linq;
using System.Security.Claims;
using ChatService.Data;
using ChatServiceDTOs.Chats;
using ChatServiceModels.Chats;
using ChatService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using Shared.IdentityServiceDTOs;
using MessageServiceDTOs;
using System;

namespace ChatService.Services
{
    public class ChatService : IChatService
    {
        private readonly ChatDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _identityClient;
        private readonly HttpClient _messageClient;

        public ChatService(ChatDbContext context, IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _identityClient = httpClientFactory.CreateClient("IdentityClient");
            _messageClient = httpClientFactory.CreateClient("MessageClient");
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

            var lastMessagePreview = await GetLastMessagePreviewByChatRoomIdAsync(groupChat.Id);

            // Формуємо DTO для відповіді
            var dto = new GroupChatRoomDto
            {
                Id = groupChat.Id,
                Name = groupChat.Name,
                CreatedAt = groupChat.CreatedAt,
                OwnerId = groupChat.OwnerId,
                LastMessagePreview = lastMessagePreview,
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

            int? partnerId = privateChatRoom.UserChatRooms.FirstOrDefault(uc => uc.UserId != currentUserId)?.UserId;
            string partnerDisplayName = partnerId.HasValue
                ? await GetUserDisplayNameByIdAsync(partnerId.Value)
                : "Unknown";
            var lastMessagePreview = await GetLastMessagePreviewByChatRoomIdAsync(privateChatRoom.Id);
            var dto = new ChatRoomDto
            {
                Id = privateChatRoom.Id,
                CreatedAt = privateChatRoom.CreatedAt,
                Name = partnerDisplayName,
                LastMessagePreview = lastMessagePreview,
                ParticipantIds = privateChatRoom.UserChatRooms.Select(uc => uc.UserId).ToList()
            };

            return dto;
        }

        public async Task<bool> DeletePrivateСhatAsync(int privateChatId)
        {
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

            var chat = await _context.PrivateChatRooms.FirstOrDefaultAsync(c => c.Id == privateChatId && (c.UserChatRooms.Any(uc => uc.UserId == currentUserId)));
            if (chat == null)
                return false;

            _context.PrivateChatRooms.Remove(chat);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteGroupСhatAsync(int privateChatId)
        {
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

            var chat = await _context.GroupChatRooms.FirstOrDefaultAsync(c => c.Id == privateChatId && (c.GroupChatMembers.Any(gcm => gcm.UserId == currentUserId)));

            if (chat == null)
                return false;

            if (chat.OwnerId != currentUserId)
                return false;

            _context.GroupChatRooms.Remove(chat);
            await _context.SaveChangesAsync();
            return true;
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
                .ToListAsync();

            var chatDtos = new List<ChatRoomDto>();


            foreach (var chat in privateChats)
            {
                var lastMessagePreview = await GetLastMessagePreviewByChatRoomIdAsync(chat.Id);
                // Якщо в чаті є саме 2 учасники, визначаємо співрозмовника
                if (chat.UserChatRooms.Count == 2)
                {
                    var partnerId = chat.UserChatRooms.FirstOrDefault(uc => uc.UserId != currentUserId)?.UserId;
                    string partnerDisplayName = partnerId.HasValue
                        ? await GetUserDisplayNameByIdAsync(partnerId.Value) // Виклик до IdentityService
                        : "Unknown";

                    chatDtos.Add(new ChatRoomDto
                    {
                        Id = chat.Id,
                        CreatedAt = chat.CreatedAt,
                        Name = partnerDisplayName,
                        LastMessagePreview = lastMessagePreview,
                        ParticipantIds = chat.UserChatRooms.Select(uc => uc.UserId)
                    });
                }
                else
                {
                    // Якщо кількість учасників не дорівнює 2, можна повернути інший формат
                    chatDtos.Add(new ChatRoomDto
                    {
                        Id = chat.Id,
                        CreatedAt = chat.CreatedAt,
                        Name = "Приватний чат",
                        LastMessagePreview = lastMessagePreview,
                        ParticipantIds = chat.UserChatRooms.Select(uc => uc.UserId)
                    });
                }
            }

            return chatDtos;
        }

        // Метод для отримання всіх групових чатів, у яких бере участь користувач
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
                .ToListAsync();

            var groupChatDtos = new List<GroupChatRoomDto>();

            foreach (var groupChat in groupChats)
            {
                var lastMessagePreview = await GetLastMessagePreviewByChatRoomIdAsync(groupChat.Id);
                groupChatDtos.Add(new GroupChatRoomDto
                {
                    Id = groupChat.Id,
                    Name = groupChat.Name,
                    CreatedAt = groupChat.CreatedAt,
                    OwnerId = groupChat.OwnerId,
                    LastMessagePreview = lastMessagePreview,
                    Members = groupChat.GroupChatMembers.Select(gm => new GroupChatMemberDto
                    {
                        UserId = gm.UserId,
                        Role = gm.Role
                    }).ToList()
                });
            }

            return groupChatDtos;
        }

        public async Task<IEnumerable<ChatRoomDto>> GetPrivateChatsForFolderAsync(int folderId)
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
                .Where(pcr => pcr.UserChatRooms.Any(puc => puc.UserId == currentUserId) && pcr.FolderId == folderId)
                .ToListAsync();

            var chatDtos = new List<ChatRoomDto>();


            foreach (var chat in privateChats)
            {
                var lastMessagePreview = await GetLastMessagePreviewByChatRoomIdAsync(chat.Id);
                // Якщо в чаті є саме 2 учасники, визначаємо співрозмовника
                if (chat.UserChatRooms.Count == 2)
                {
                    var partnerId = chat.UserChatRooms.FirstOrDefault(uc => uc.UserId != currentUserId)?.UserId;
                    string partnerDisplayName = partnerId.HasValue
                        ? await GetUserDisplayNameByIdAsync(partnerId.Value) // Виклик до IdentityService
                        : "Unknown";
                    chatDtos.Add(new ChatRoomDto
                    {
                        Id = chat.Id,
                        CreatedAt = chat.CreatedAt,
                        Name = partnerDisplayName,
                        LastMessagePreview = lastMessagePreview,
                        ParticipantIds = chat.UserChatRooms.Select(uc => uc.UserId)
                    });
                }
                else
                {
                    // Якщо кількість учасників не дорівнює 2, можна повернути інший формат
                    chatDtos.Add(new ChatRoomDto
                    {
                        Id = chat.Id,
                        CreatedAt = chat.CreatedAt,
                        Name = "Приватний чат",
                        LastMessagePreview = lastMessagePreview,
                        ParticipantIds = chat.UserChatRooms.Select(uc => uc.UserId)
                    });
                }
            }

            return chatDtos;
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

            // Отримуємо приватні чати, де поточний користувач бере участь
            var privateChats = await _context.PrivateChatRooms
                .Include(pcr => pcr.UserChatRooms)
                .Where(pcr => pcr.UserChatRooms.Any(puc => puc.UserId == currentUserId) && pcr.FolderId == null)
                .ToListAsync();

            var chatDtos = new List<ChatRoomDto>();


            foreach (var chat in privateChats)
            {
                var lastMessagePreview = await GetLastMessagePreviewByChatRoomIdAsync(chat.Id);
                // Якщо в чаті є саме 2 учасники, визначаємо співрозмовника
                if (chat.UserChatRooms.Count == 2)
                {
                    var partnerId = chat.UserChatRooms.FirstOrDefault(uc => uc.UserId != currentUserId)?.UserId;
                    string partnerDisplayName = partnerId.HasValue
                        ? await GetUserDisplayNameByIdAsync(partnerId.Value) // Виклик до IdentityService
                        : "Unknown";

                    chatDtos.Add(new ChatRoomDto
                    {
                        Id = chat.Id,
                        CreatedAt = chat.CreatedAt,
                        Name = partnerDisplayName,
                        LastMessagePreview = lastMessagePreview,
                        ParticipantIds = chat.UserChatRooms.Select(uc => uc.UserId)
                    });
                }
                else
                {
                    // Якщо кількість учасників не дорівнює 2, можна повернути інший формат
                    chatDtos.Add(new ChatRoomDto
                    {
                        Id = chat.Id,
                        CreatedAt = chat.CreatedAt,
                        Name = "Private chat",
                        LastMessagePreview = lastMessagePreview,
                        ParticipantIds = chat.UserChatRooms.Select(uc => uc.UserId)
                    });
                }
            }

            return chatDtos;
        }

        public async Task<IEnumerable<GroupChatRoomDto>> GetGroupChatsWithoutFolderAsync()
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
                .Where(gr => gr.GroupChatMembers.Any(gc => gc.UserId == currentUserId) && gr.FolderId == null)
                .ToListAsync();

            var groupChatDtos = new List<GroupChatRoomDto>();

            foreach (var groupChat in groupChats)
            {
                var lastMessagePreview = await GetLastMessagePreviewByChatRoomIdAsync(groupChat.Id);
                groupChatDtos.Add(new GroupChatRoomDto
                {
                    Id = groupChat.Id,
                    Name = groupChat.Name,
                    CreatedAt = groupChat.CreatedAt,
                    OwnerId = groupChat.OwnerId,
                    LastMessagePreview = lastMessagePreview,
                    Members = groupChat.GroupChatMembers.Select(gm => new GroupChatMemberDto
                    {
                        UserId = gm.UserId,
                        Role = gm.Role
                    }).ToList()
                });
            }

            return groupChatDtos;
        }

        // Метод для отримання LastMessagePreview через MessageService
        private async Task<MessageDto> GetLastMessagePreviewByChatRoomIdAsync(int chatRoomId)
        {
            var response = await _messageClient.GetFromJsonAsync<MessageDto>($"api/message/get-last-message/{chatRoomId}");
            if( response == null ) {
                return new MessageDto { Content = "No messages yet" };
            }
            return response;
        }

        // Метод для отримання DisplayName співрозмовника через IdentityService
        private async Task<string> GetUserDisplayNameByIdAsync(int userId)
        {
            var response = await _identityClient.GetFromJsonAsync<UserDto>($"api/users/search/id/{userId}");
            return response?.DisplayName ?? "Unknown";
        }

        public async Task<bool> IsAuthUserInChatRoomsByChatRoomIdAsync(int chatRoomId)
        {
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

            var chatRoom = await _context.PrivateChatRooms
                .Where(pcr => pcr.UserChatRooms.Any(puc => puc.UserId == currentUserId))
                .Include(cr => cr.UserChatRooms)
                .FirstOrDefaultAsync(cr => cr.Id == chatRoomId);

            var groupChatRoom = await _context.GroupChatRooms
                .Where(gcr => gcr.GroupChatMembers.Any(gcm => gcm.UserId == currentUserId))
                .Include(cr => cr.GroupChatMembers)
                .FirstOrDefaultAsync(cr => cr.Id == chatRoomId);

            if (chatRoom == null && groupChatRoom == null)
                return false;

            return true;
        }
    }
}
