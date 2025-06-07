using ChatService.Data;
using ChatService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;
using ChatService.Models;
using ChatService.Mappers.Interfaces;
using System;
using Shared.DTOs.Chat;
using Shared.DTOs.Identity;
using Shared.DTOs.Message;
using ChatService.Services.Interfaces;
using Grpc.Core;

namespace ChatService.Repositories
{
    public class ChatRoomRepository : IChatRoomRepository
    {
        private readonly ChatDbContext _context;
        private readonly IIdentityGrpcService _identityGrpcService;
        private readonly IMessageGrpcService _messageInfoService;
        private readonly IMapperFactory _mapperFactory;

        public ChatRoomRepository(
            ChatDbContext context,
            IIdentityGrpcService identityGrpcService,
            IMessageGrpcService messageInfoService,
            IMapperFactory mapperFactory)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _identityGrpcService = identityGrpcService ?? throw new ArgumentNullException(nameof(identityGrpcService));
            _messageInfoService = messageInfoService ?? throw new ArgumentNullException(nameof(messageInfoService));
            _mapperFactory = mapperFactory ?? throw new ArgumentNullException(nameof(mapperFactory));
        }

        public async Task<ChatRoomType> GetChatRoomTypeByIdAsync(int chatRoomId)
        {
            try
            {
                var chat = await _context.ChatRooms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(cr => cr.Id == chatRoomId);
                if (chat == null)
                {
                    throw new EntityNotFoundException("ChatRoom", chatRoomId);
                }
                return chat.ChatRoomType;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Помилка при отриманні типу чату", ex);
            }
        }

        public async Task<ChatRoomDto?> GetPrivateChatByIdAsync(int chatRoomId)
        {
            try
            {
                if(!await CheckIfChatExistsAsync(chatRoomId))
                {
                    throw new EntityNotFoundException("PrivateChatRoom", chatRoomId);
                }

                var chat = await _context.PrivateChatRooms
                    .Include(pcr => pcr.UserChatRooms)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(pcr => pcr.Id == chatRoomId);

                var lastMessagePreview = await GetLastMessagePreviewAsync(chatRoomId);

                return await _mapperFactory.GetMapper<PrivateChatRoom, ChatRoomDto>().MapToDtoAsync(chat);
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Помилка при отриманні приватного чату", ex);
            }
        }

        public async Task<GroupChatRoomDto?> GetGroupChatByIdAsync(int chatRoomId)
        {
            try
            {
                if (!await CheckIfChatExistsAsync(chatRoomId))
                {
                    throw new EntityNotFoundException("GroupChatRoom", chatRoomId);
                }

                var chat = await _context.GroupChatRooms
                    .Include(gcr => gcr.GroupChatMembers)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(gcr => gcr.Id == chatRoomId);

                var lastMessagePreview = await GetLastMessagePreviewAsync(chatRoomId);

                return await _mapperFactory.GetMapper<GroupChatRoom, GroupChatRoomDto>().MapToDtoAsync(chat);
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Помилка при отриманні групового чату", ex);
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

                if (privateChats == null || !privateChats.Any())
                {
                    throw new EntityNotFoundException("PrivateChatRooms");
                }

                return await _mapperFactory.GetMapper<PrivateChatRoom, ChatRoomDto>().MapToDtoCollectionAsync(privateChats, userId);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Помилка при отриманні приватних чатів", ex);
            }
        }

        public async Task<bool> UserBelongsToChatAsync(int userId, int chatRoomId)
        {
            try
            {
                // Перевіряємо, чи чат існує
                if (!await CheckIfChatExistsAsync(chatRoomId))
                {
                    throw new EntityNotFoundException("ChatRoom", chatRoomId);
                }

                // Перевіряємо в приватних чатах
                bool isPrivateChatMember = await _context.UserChatRooms
                    .AnyAsync(u => u.PrivateChatRoomId == chatRoomId && u.UserId == userId);

                if (isPrivateChatMember) return true;

                // Перевіряємо в групових чатах
                bool isGroupChatMember = await _context.GroupChatMembers
                    .AnyAsync(g => g.GroupChatRoomId == chatRoomId && g.UserId == userId);

                return isGroupChatMember;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Помилка при перевірці чи належить користувач до чату", ex);
            }
        }

        public async Task<ChatRoomDto> CreatePrivateChatAsync(CreatePrivateChatRoomDto dto, int currentUserId)
        {
            try
            {
                // Перевіряємо, чи не існує вже чат між цими користувачами
                var existingChat = await _context.PrivateChatRooms
                    .Include(pcr => pcr.UserChatRooms)
                    .AsNoTracking()
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
                var privateChatRoom = new PrivateChatRoom
                {
                    ChatRoomType = ChatRoomType.privateChat,
                    CreatedAt = DateTime.UtcNow,
                    UserChatRooms = new List<UserChatRoom>
                    {
                        new() { UserId = currentUserId },
                        new() { UserId = dto.UserId }
                    }
                };

                // Зберігаємо в базу даних
                _context.PrivateChatRooms.Add(privateChatRoom);
                await _context.SaveChangesAsync();

                // Формуємо відповідь
                return await _mapperFactory.GetMapper<PrivateChatRoom, ChatRoomDto>().MapToDtoAsync(privateChatRoom, currentUserId);
            }
            catch (DbUpdateException ex)
            {
                throw new DatabaseException("Помилка при створенні приватного чату", ex);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Несподівана помилка при створенні приватного чату", ex);
            }
        }

        public async Task<bool> DeleteChatAsync(int chatRoomId)
        {
            try
            {
                // Перевіряємо, чи чат існує
                if (!await CheckIfChatExistsAsync(chatRoomId))
                {
                    throw new EntityNotFoundException("ChatRoom", chatRoomId);
                }

                var chat = await _context.ChatRooms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == chatRoomId);

                _context.ChatRooms.Remove(chat);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                throw new DatabaseException("Помилка при видаленні чату", ex);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Несподівана помилка при видаленні чату", ex);
            }
        }

        public async Task<bool> DeletePrivateChatAsync(int chatRoomId)
        {
            try
            {
                if (!await CheckIfChatExistsAsync(chatRoomId))
                {
                    throw new EntityNotFoundException("ChatRoom", chatRoomId);
                }

                var chat = await _context.PrivateChatRooms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == chatRoomId);

                _context.PrivateChatRooms.Remove(chat);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                throw new DatabaseException("Помилка при видаленні приватного чату", ex);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Несподівана помилка при видаленні чату", ex);
            }
        }

        public async Task<IEnumerable<GroupChatRoomDto>> GetGroupChatsForUserAsync(int userId)
        {
            try
            {
                var groupChats = _context.GroupChatRooms
                    .Include(gcr => gcr.GroupChatMembers)
                    .Where(gcr => gcr.GroupChatMembers.Any(gcm => gcm.UserId == userId))
                    .AsNoTracking()
                    .ToList();

                if (groupChats == null || !groupChats.Any())
                {
                    throw new EntityNotFoundException("GroupChatRooms");
                }

                return await _mapperFactory.GetMapper<GroupChatRoom, GroupChatRoomDto>().MapToDtoCollectionAsync(groupChats, userId);
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Помилка при перевірці доступу до групових чатів", ex);
            }
        }

        public async Task<IEnumerable<ChatRoomDto>> GetPrivateChatsForFolderAsync(int folderId)
        {
            try
            {
                var privateChats = _context.PrivateChatRooms
                    .Include(pcr => pcr.UserChatRooms)
                    .Where(pcr => pcr.FolderId == folderId)
                    .AsNoTracking()
                    .ToList();

                if (privateChats == null || !privateChats.Any())
                {
                    throw new EntityNotFoundException("PrivateChatRooms");
                }
                return await _mapperFactory.GetMapper<PrivateChatRoom, ChatRoomDto>().MapToDtoCollectionAsync(privateChats);
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Помилка при перевірці доступу до приватних чатів для папки", ex);
            }
        }

        public async Task<IEnumerable<GroupChatRoomDto>> GetGroupChatsForFolderAsync(int folderId)
        {
            try
            {
                var groupChats = _context.GroupChatRooms
                    .Include(gcr => gcr.GroupChatMembers)
                    .Where(gcr => gcr.FolderId == folderId)
                    .AsNoTracking()
                    .ToList();

                if (groupChats == null || !groupChats.Any())
                {
                    throw new EntityNotFoundException("GroupChatRooms");
                }
                return await _mapperFactory.GetMapper<GroupChatRoom, GroupChatRoomDto>().MapToDtoCollectionAsync(groupChats);
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Помилка при перевірці доступу до групових чатів для папки", ex);
            }
        }

        public async Task<IEnumerable<ChatRoomDto>> GetPrivateChatsWithoutFolderAsync(int userId)
        {
            try
            {
                var privateChats = _context.PrivateChatRooms
                    .Include(pcr => pcr.UserChatRooms)
                    .Where(pcr => pcr.UserChatRooms.Any(ucr => ucr.UserId == userId) && pcr.FolderId == null)
                    .AsNoTracking()
                    .ToList();

                if (privateChats == null || !privateChats.Any())
                {
                    throw new EntityNotFoundException("PrivateChatRooms");
                }
                return await _mapperFactory.GetMapper<PrivateChatRoom, ChatRoomDto>().MapToDtoCollectionAsync(privateChats, userId);
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Помилка при перевірці доступу до приватних чатів без папки", ex);
            }
        }

        public async Task<IEnumerable<GroupChatRoomDto>> GetGroupChatsWithoutFolderAsync(int userId)
        {
            try
            {
                var groupChats = _context.GroupChatRooms
                    .Include(gcr => gcr.GroupChatMembers)
                    .Where(gcr => gcr.GroupChatMembers.Any(gcm => gcm.UserId == userId) && gcr.FolderId == null)
                    .AsNoTracking()
                    .ToList();

                if (groupChats == null || !groupChats.Any())
                {
                    throw new EntityNotFoundException("GroupChatRooms");
                }
                return await _mapperFactory.GetMapper<GroupChatRoom, GroupChatRoomDto>().MapToDtoCollectionAsync(groupChats);
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Помилка при перевірці доступу до групових чатів без папки", ex);
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
                    .AsNoTracking()
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

                return await _mapperFactory.GetMapper<GroupChatRoom, GroupChatRoomDto>().MapToDtoAsync(groupChatRoom);
            }
            catch (DbUpdateException ex)
            {
                throw new DatabaseException("Помилка при створенні групового чату", ex);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Несподівана помилка при створенні групового чату", ex);
            }
        }

        public async Task<bool> DeleteGroupChatAsync(int chatRoomId)
        {
            try
            {
                var chat = await _context.GroupChatRooms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == chatRoomId);

                if (chat == null)
                {
                    return false;
                }

                _context.GroupChatRooms.Remove(chat);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                throw new DatabaseException("Помилка при видаленні групового чату", ex);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Несподівана помилка при видаленні групового чату", ex);
            }
        }

        public async Task<bool> CanAccessPrivateChatAsync(int userId, int chatRoomId)
        {
            try
            {
                var hasAccess = await _context.UserChatRooms
                    .AsNoTracking()
                    .AnyAsync(ucr => ucr.PrivateChatRoomId == chatRoomId && ucr.UserId == userId);

                return hasAccess;
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Помилка при перевірці доступу до приватного чату", ex);
            }
        }

        public async Task<bool> CanAccessGroupChatAsync(int userId, int chatRoomId)
        {
            try
            {
                var hasAccess = await _context.GroupChatMembers
                    .AsNoTracking()
                    .AnyAsync(gcm => gcm.GroupChatRoomId == chatRoomId && gcm.UserId == userId);

                return hasAccess;
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Помилка при перевірці доступу до приватного чату", ex);
            }
        }

        public async Task<int> GetOwnerGroupChatAsync(int chatRoomId)
        {
            try
            {
                var chat = await _context.GroupChatRooms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == chatRoomId);

                if (chat == null)
                {
                    throw new EntityNotFoundException("GroupChatRoom", chatRoomId);
                }

                return chat.OwnerId;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Помилка при отриманні власника групового чату", ex);
            }
        }

        public async Task<GroupRole> GetUserRoleInGroupChatAsync(int userId, int chatRoomId)
        {
            try
            {
                if (!await CheckIfChatExistsAsync(chatRoomId))
                {
                    throw new EntityNotFoundException("PrivateChatRoom", chatRoomId);
                }

                var chatMember = await _context.GroupChatMembers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(gcm => gcm.GroupChatRoomId == chatRoomId && gcm.UserId == userId);

                if (chatMember == null)
                {
                    throw new EntityNotFoundException("GroupChatMember", userId);
                }

                return chatMember.Role;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Помилка при отриманні ролі учасника групового чату", ex);
            }
        }

        public async Task<List<int>> GetChatParticipantsFromPrivateChatAsync(int chatRoomId)
        {
            try
            {
                // Перевіряємо, чи чат існує
                if (!await CheckIfChatExistsAsync(chatRoomId))
                {
                    throw new EntityNotFoundException("PrivateChatRoom", chatRoomId);
                }

                var chat = await _context.PrivateChatRooms
                .AsNoTracking()
                .Include(pcr => pcr.UserChatRooms)
                .FirstOrDefaultAsync(pcr => pcr.Id == chatRoomId);

                var participants = chat.UserChatRooms
                    .Select(ucr => ucr.UserId)
                    .ToList();

                return participants;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Помилка при отриманні учасників приватного чату", ex);
            }
        }

        public async Task<List<int>> GetChatParticipantsFromGroupChatAsync(int chatRoomId)
        {
            try
            {
                // Перевіряємо, чи чат існує
                if (!await CheckIfChatExistsAsync(chatRoomId))
                {
                    throw new EntityNotFoundException("GroupChatRoom", chatRoomId);
                }

                var chat = await _context.GroupChatRooms
                    .AsNoTracking()
                    .Include(gcr => gcr.GroupChatMembers)
                    .FirstOrDefaultAsync(gcr => gcr.Id == chatRoomId);

                var participants = chat.GroupChatMembers
                    .Select(gcm => gcm.UserId)
                    .ToList();

                return participants;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Помилка при отриманні учасників групового чату", ex);
            }
        }

        public async Task<bool> CheckIfChatExistsAsync(int chatRoomId)
        {
            try
            {
                return await _context.ChatRooms
                    .AsNoTracking()
                    .AnyAsync(cr => cr.Id == chatRoomId);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
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
                var userDto = await _identityGrpcService.GetUserInfoAsync(partnerId.Value);
                return userDto?.DisplayName ?? $"Користувач {partnerId.Value}";
            }
            catch (Exception)
            {
                return $"Користувач {partnerId.Value}";
            }
        }

        public async Task<MessageDto> GetLastMessagePreviewAsync(int chatRoomId)
        {
            try
            {
                // Використовуємо gRPC-сервіс замість HTTP-запиту
                return await _messageInfoService.GetLastMessageAsync(chatRoomId) ?? new MessageDto();
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Помилка при отриманні попереднього повідомлення", ex);
            }
        }
    }
}
