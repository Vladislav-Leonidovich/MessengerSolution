using ChatService.Models;
using Shared.DTOs.Chat;
using Shared.DTOs.Responses;

namespace ChatService.Services.Interfaces
{
    public interface IChatService
    {
        // Методи для всіх чатів
        Task<ApiResponse<object>> GetChatByIdAsync(int chatRoomId, int userId);
        Task<ApiResponse<bool>> DeleteChatByIdAsync(int chatRoomId, int userId);

        // Методи для приватних чатів GET
        Task<ApiResponse<ChatRoomDto>> GetPrivateChatByIdAsync(int chatRoomId, int userId);
        Task<ApiResponse<IEnumerable<ChatRoomDto>>> GetPrivateChatsForUserAsync(int userId);
        Task<ApiResponse<IEnumerable<ChatRoomDto>>> GetPrivateChatsForFolderAsync(int folderId, int userId);
        Task<ApiResponse<IEnumerable<ChatRoomDto>>> GetPrivateChatsWithoutFolderAsync(int userId);

        // Методи для приватних чатів CREATE
        Task<ApiResponse<ChatRoomDto>> CreatePrivateChatAsync(CreatePrivateChatRoomDto dto, int userId);

        // Методи для приватних чатів DELETE
        Task<ApiResponse<bool>> DeletePrivateChatByIdAsync(int chatRoomId, int userId);


        // Методи для групових чатів GET
        Task<ApiResponse<GroupChatRoomDto>> GetGroupChatByIdAsync(int chatRoomId, int userId);
        Task<ApiResponse<IEnumerable<GroupChatRoomDto>>> GetGroupChatsForUserAsync(int userId);
        Task<ApiResponse<IEnumerable<GroupChatRoomDto>>> GetGroupChatsForFolderAsync(int folderId, int userId);
        Task<ApiResponse<IEnumerable<GroupChatRoomDto>>> GetGroupChatsWithoutFolderAsync(int userId);

        // Методи для групових чатів CREATE
        Task<ApiResponse<GroupChatRoomDto>> CreateGroupChatAsync(CreateGroupChatRoomDto dto, int userId);

        // Методи для групових чатів DELETE
        Task<ApiResponse<bool>> DeleteGroupChatByIdAsync(int chatRoomId, int userId);

        // Інше
        Task<bool> IsUserInChatAsync(int userId, int chatRoomId);
    }
}
