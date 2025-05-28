using Shared.DTOs.Chat;
using Shared.DTOs.Message;

namespace ChatService.Repositories.Interfaces
{
    public interface IChatRoomRepository
    {
        Task<ChatRoomType> GetChatRoomTypeByIdAsync(int chatRoomId);
        Task<ChatRoomDto?> GetPrivateChatByIdAsync(int chatRoomId);
        Task<GroupChatRoomDto?> GetGroupChatByIdAsync(int chatRoomId);
        Task<IEnumerable<ChatRoomDto>> GetPrivateChatsForUserAsync(int userId);
        Task<IEnumerable<GroupChatRoomDto>> GetGroupChatsForUserAsync(int userId);
        Task<IEnumerable<ChatRoomDto>> GetPrivateChatsForFolderAsync(int folderId);
        Task<IEnumerable<GroupChatRoomDto>> GetGroupChatsForFolderAsync(int folderId);
        Task<IEnumerable<ChatRoomDto>> GetPrivateChatsWithoutFolderAsync(int userId);
        Task<IEnumerable<GroupChatRoomDto>> GetGroupChatsWithoutFolderAsync(int userId);
        Task<ChatRoomDto> CreatePrivateChatAsync(CreatePrivateChatRoomDto dto, int currentUserId);
        Task<GroupChatRoomDto> CreateGroupChatAsync(CreateGroupChatRoomDto dto, int currentUserId);
        Task<bool> DeleteChatAsync(int chatRoomId);
        Task<bool> DeletePrivateChatAsync(int chatRoomId);
        Task<bool> DeleteGroupChatAsync(int chatRoomId);
        Task<bool> UserBelongsToChatAsync(int userId, int chatRoomId);
        Task<bool> CanAccessPrivateChatAsync(int userId, int chatRoomId);
        Task<bool> CanAccessGroupChatAsync(int userId, int chatRoomId);
        Task<bool> CheckIfChatExistsAsync(int chatRoomId);
        Task<int> GetOwnerGroupChatAsync(int chatRoomId);
        Task<GroupRole> GetUserRoleInGroupChatAsync(int userId, int chatRoomId);
        Task<List<int>> GetChatParticipantsFromPrivateChatAsync(int chatRoomId);
        Task<List<int>> GetChatParticipantsFromGroupChatAsync(int chatRoomId);
        Task<MessageDto> GetLastMessagePreviewAsync(int chatRoomId);
    }
}
